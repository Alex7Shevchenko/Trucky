using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class AssetOrganizer : EditorWindow
{
    private const float ITEM_HEIGHT = 24f;
    private const float DROP_ZONE_HEIGHT = 4f;
    private const float HOVER_THRESHOLD = 8f;
    private const float AUTO_EXPAND_DELAY = 0.3f;
    private const float CATEGORY_HOVER_DELAY = 0.8f;

    private static readonly Color LIGHT_RED = new Color(1f, 0.5f, 0.5f);
    private static readonly Color DROP_ZONE_HIGHLIGHT = new Color(0.5f, 0.8f, 1f, 0.6f);
    private static readonly Color DROP_ZONE_SUBTLE = new Color(0.5f, 0.5f, 0.5f, 0.2f);

    private static string PROJECT_PREFIX => Application.dataPath.GetHashCode().ToString();
    private static string ITEMS_KEY => $"AssetsEditor_{PROJECT_PREFIX}_Items";
    private static string CATEGORIES_KEY => $"AssetsEditor_{PROJECT_PREFIX}_Categories";
    private static string SLIDER_KEY => $"AssetsEditor_Slider_{PROJECT_PREFIX}";
    private static string FOLDOUT_KEY => $"AssetsEditor_Foldout_{PROJECT_PREFIX}";

    private List<AssetItem> _assets = new List<AssetItem>();
    private List<AssetCategory> _categories = new List<AssetCategory>();

    private Vector2 _scrollPosition;
    private string _newCategoryName = null;
    private bool _showAddCategoryInput = false;
    private bool _showFoldersDropdown = false;
    private bool _potentialClick;
    private AssetCategory _categoryBeingRenamed = null;
    private string _renameBuffer = null;

    private AssetItem _draggedItem = null;
    private Vector2 _dragOffset;
    private bool _isDragging = false;
    private float _dragStartTime = 0f;
    private HashSet<string> _autoExpandedCategories = new HashSet<string>();
    private string _hoveredCategoryForExpand = null;
    private float _categoryHoverStartTime = 0f;

    private float _sliderCurrentValue = 1;
    private float _sliderMinValue = 10f;
    private float _sliderMaxValue = 20f;

    [MenuItem("Tools/Asset Organizer/Open", false, 0)]
    public static void ShowWindow()
    {
        AssetOrganizer window = GetWindow<AssetOrganizer>("Asset Organizer");
        window.minSize = new Vector2(250, 300);
        window.LoadDataFromPrefs();
    }

    [MenuItem("Tools/Asset Organizer/Clear Saved Data", false, 50)]
    private static void ClearOrganizerPrefs()
    {
        string title = "Clear Saved Data";
        string message = "You are about to clear all organizer stored data\n\nAre you sure?";
        string confirm = "Clear Data";
        string cancel = "Cancel";

        if (EditorUtility.DisplayDialog(title, message, confirm, cancel))
        {
            if (EditorPrefs.HasKey(ITEMS_KEY))
                EditorPrefs.DeleteKey(ITEMS_KEY);
            if (EditorPrefs.HasKey(CATEGORIES_KEY))
                EditorPrefs.DeleteKey(CATEGORIES_KEY);
        }
    }

    private void OnEnable()
    {
        if (this == null)
            return;
        LoadDataFromPrefs();
        Repaint();
    }

    private void OnDisable() => SaveDataToPrefs();

    private void OnGUI()
    {
        HandleExternalDragAndDrop();
        DrawToolbar();
        DrawCategoryCreationSection();
        RenderAssetsScrollArea();
        DrawDragOverlay();
        HandleItemDragging();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Add Category", EditorStyles.toolbarButton, GUILayout.Width(85)))
                _showAddCategoryInput = !_showAddCategoryInput;

            if (GUILayout.Button("Remove All Assets", EditorStyles.toolbarButton, GUILayout.Width(115)))
                ConfirmRemoveAllAssets();

            if (GUILayout.Button("Remove All", EditorStyles.toolbarButton, GUILayout.Width(75)))
                ConfirmRemoveEverything();

            _showFoldersDropdown = EditorGUILayout.ToggleLeft("Folders Dropdown", _showFoldersDropdown, GUILayout.Width(125));

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65)))
                RefreshAllAssetReferences();

            GUILayout.FlexibleSpace();
        }
    }

    private void DrawCategoryCreationSection()
    {
        _sliderMinValue = 0.6f;
        _sliderMaxValue = 1.5f;
        _sliderCurrentValue = EditorGUILayout.Slider("Assets Size", _sliderCurrentValue, _sliderMinValue, _sliderMaxValue);

        if (!_showAddCategoryInput)
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            _newCategoryName = EditorGUILayout.TextField("Category Name:", _newCategoryName, GUILayout.MinWidth(216));

            if (GUILayout.Button("Add", GUILayout.Width(40)) && !string.IsNullOrEmpty(_newCategoryName))
            {
                AddCategory(_newCategoryName);
                _newCategoryName = "";
                _showAddCategoryInput = false;
            }
        }
        EditorGUILayout.Space();
    }

    private void RenderAssetsScrollArea()
    {
        using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
        {
            _scrollPosition = scrollView.scrollPosition;

            HandleDragAutoExpand();
            DrawCategoriesAndItems();
        }
    }

    private void DrawCategoriesAndItems()
    {
        GUILayout.Space(5);

        foreach (var category in _categories.ToList())
        {
            DrawCategorySection(category);
            GUILayout.Space(3);
        }

        DrawUnsortedSection();
    }

    private void DrawCategorySection(AssetCategory category)
    {
        var originalColor = GUI.color;
        GUI.color = category.color;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.color = originalColor;

            DrawCategoryHeader(category);
            HandleCategoryHoverForAutoExpand(category);

            if (category.isExpanded)
                DrawCategoryContents(category);

            HandleEmptyCategoryDropZone(category);
        }
    }

    private void DrawCategoryHeader(AssetCategory category)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(25)))
        {
            DrawColorPicker(category);
            DrawCategoryFoldout(category);
            DrawCategoryRenameUI(category);
            DrawCategoryOptionsButton(category);
        }
    }

    private void DrawColorPicker(AssetCategory category)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(20)))
        {
            EditorGUI.BeginChangeCheck();
            category.color = EditorGUILayout.ColorField(GUIContent.none, category.color, false, false, false, GUILayout.Width(13), GUILayout.Height(18));

            if (EditorGUI.EndChangeCheck())
                SaveDataToPrefs();
        }
    }

    private void DrawCategoryFoldout(AssetCategory category)
    {
        var itemCount = _assets.Count(f => f.categoryName == category.name);
        var foldoutStyle = new GUIStyle(EditorStyles.foldout) { richText = true };

        using (new EditorGUILayout.VerticalScope())
        {
            category.isExpanded = EditorGUILayout.Foldout(category.isExpanded, new GUIContent($"<b>{category.name} - {itemCount}</b>"), true, foldoutStyle);
        }
    }

    private void DrawCategoryRenameUI(AssetCategory category)
    {
        if (_categoryBeingRenamed != category)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _renameBuffer = EditorGUILayout.TextField(_renameBuffer);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rename"))
                    ConfirmRename(category);

                if (GUILayout.Button("Cancel"))
                    CancelRename();
            }
        }
    }

    private void DrawCategoryOptionsButton(AssetCategory category)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(20)))
        {
            var buttonStyle = new GUIStyle
            {
                richText = true,
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (GUILayout.Button("<b>⋮</b>", buttonStyle))
                ShowCategoryContextMenu(category);
        }
    }

    private void DrawCategoryContents(AssetCategory category)
    {
        var itemsInCategory = _assets.Where(f => f.categoryName == category.name).ToList();

        if (itemsInCategory.Count > 0)
        {
            for (int i = 0; i < itemsInCategory.Count; i++)
                DrawAssetItem(itemsInCategory[i], category.name, i);
        }
    }

    private void DrawUnsortedSection()
    {
        var unsortedItems = _assets.Where(f => string.IsNullOrEmpty(f.categoryName)).ToList();

        if (unsortedItems.Count == 0 && (!_isDragging || _draggedItem == null))
            return;

        var originalColor = GUI.color;
        GUI.color = Color.black;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.color = originalColor;

            if (_categories.Count != 0)
                EditorGUILayout.LabelField("Unsorted", EditorStyles.boldLabel);

            if (unsortedItems.Count > 0)
            {
                for (int i = 0; i < unsortedItems.Count; i++)
                    DrawAssetItem(unsortedItems[i], "", i);
            }
            else if (_isDragging && _draggedItem != null)
            {
                DrawEmptyAreaDropZone("", "Drop here to unsort asset");
            }
        }
    }

    private void DrawAssetItem(AssetItem item, string categoryName, int indexInCategory)
    {
        if (item.asset == null)
        {
            _assets.Remove(item);
            SaveDataToPrefs();
            return;
        }

        bool isBeingDragged = _isDragging && _draggedItem == item;

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDragHandle(isBeingDragged);
            DrawItemButton(item, isBeingDragged);
            DrawAssetCategoryDropdown(item, isBeingDragged);
            DrawRemoveButton(item, isBeingDragged);
        }

        Rect itemRect = GUILayoutUtility.GetLastRect();
        HandleItemMouseEvents(item, itemRect);
        HandleDropZoneDetection(itemRect, categoryName, indexInCategory);
    }

    private void DrawDragHandle(bool isBeingDragged)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(15 * _sliderCurrentValue), GUILayout.Height(23 * _sliderCurrentValue)))
        {
            var originalColor = GUI.color;

            if (isBeingDragged)
                GUI.color = new Color(1f, 1f, 1f, 0.3f);

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                fontSize = (int)(16f * _sliderCurrentValue)
            };

            GUILayout.Label("≡", labelStyle, GUILayout.Width(13 * _sliderCurrentValue), GUILayout.Height(14 * _sliderCurrentValue));
            GUI.color = originalColor;
        }
    }

    private void DrawItemButton(AssetItem item, bool isBeingDragged)
    {
        var icon = AssetPreview.GetMiniThumbnail(item.asset);
        var content = new GUIContent(item.asset.name, icon);

        Color originalColor = GUI.color;
        if (isBeingDragged)
            GUI.color = new Color(1f, 1f, 1f, 0.3f);

        Rect labelRect = GUILayoutUtility.GetRect
        (
            content,
            EditorStyles.label,
            GUILayout.Height(ITEM_HEIGHT * _sliderCurrentValue),
            GUILayout.MinWidth(120 * _sliderCurrentValue)
        );
        GUI.Label(labelRect, content, EditorStyles.label);

        Event e = Event.current;

        if (labelRect.Contains(e.mousePosition))
        {
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && e.clickCount == 2:
                    AssetDatabase.OpenAsset(item.asset);
                    _potentialClick = false;
                    e.Use();
                    break;

                case EventType.MouseDown when e.button == 0:
                    _potentialClick = true;
                    e.Use();
                    break;

                case EventType.MouseDrag when e.button == 0 && _potentialClick:
                    _potentialClick = false;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { item.asset };
                    DragAndDrop.StartDrag(item.asset.name);
                    e.Use();
                    break;

                case EventType.MouseUp when e.button == 0 && _potentialClick:
                    Selection.activeObject = item.asset;
                    EditorGUIUtility.PingObject(item.asset);
                    _potentialClick = false;
                    e.Use();
                    break;
            }
        }

        GUI.color = originalColor;
    }

    private void DrawAssetCategoryDropdown(AssetItem item, bool isBeingDragged)
    {
        if (_categories.Count == 0 || !_showFoldersDropdown)
            return;

        var categoryOptions = new[] { "Unsorted" }.Concat(_categories.Select(c => c.name)).ToArray();
        int currentIndex = string.IsNullOrEmpty(item.categoryName) ? 0 : System.Array.IndexOf(categoryOptions, item.categoryName);

        EditorGUI.BeginDisabledGroup(isBeingDragged);
        int newIndex = EditorGUILayout.Popup(currentIndex, categoryOptions, GUILayout.Width(80), GUILayout.Height(23 * _sliderCurrentValue));
        EditorGUI.EndDisabledGroup();

        if (newIndex != currentIndex && newIndex >= 0 && !isBeingDragged)
        {
            item.categoryName = (newIndex == 0) ? "" : categoryOptions[newIndex];
            SaveDataToPrefs();
        }
    }

    private void DrawRemoveButton(AssetItem item, bool isBeingDragged)
    {
        EditorGUI.BeginDisabledGroup(isBeingDragged);

        var originalColor = GUI.color;
        GUI.color = LIGHT_RED;

        var labelStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = (int)(16f * _sliderCurrentValue)
        };

        if (GUILayout.Button("X", labelStyle, GUILayout.Width(20 * _sliderCurrentValue), GUILayout.Height(23 * _sliderCurrentValue)))
        {
            _assets.Remove(item);
            SaveDataToPrefs();
        }

        GUI.color = originalColor;
        EditorGUI.EndDisabledGroup();
    }

    private void HandleEmptyCategoryDropZone(AssetCategory category)
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true));
        var itemsInCategory = _assets.Where(f => f.categoryName == category.name).ToList();

        if (!_isDragging || _draggedItem == null || !category.isExpanded || itemsInCategory.Count > 0)
            return;

        dropArea.height = DROP_ZONE_HEIGHT + 3;
        dropArea.x += 15;
        dropArea.width -= 15;
        GUILayout.Space(DROP_ZONE_HEIGHT + 3);

        bool isHovering = dropArea.Contains(Event.current.mousePosition);
        var color = isHovering ? DROP_ZONE_HIGHLIGHT : DROP_ZONE_SUBTLE;

        EditorGUI.DrawRect(dropArea, color);

        if (isHovering && Event.current.type == EventType.MouseUp)
        {
            MoveItemToCategory(_draggedItem, category.name, 0);
            Event.current.Use();
        }

        if (isHovering) Repaint();
    }

    private void DrawEmptyAreaDropZone(string categoryName, string message)
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        bool isHovering = dropArea.Contains(Event.current.mousePosition);
        var backgroundColor = isHovering ? new Color(0.5f, 0.8f, 1f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.1f);
        EditorGUI.DrawRect(dropArea, backgroundColor);

        if (isHovering && Event.current.type == EventType.MouseUp)
        {
            MoveItemToCategory(_draggedItem, categoryName, 0);
            Event.current.Use();
        }

        var labelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic
        };

        var originalColor = GUI.color;
        GUI.color = isHovering ? Color.white : Color.gray;
        GUI.Label(dropArea, message, labelStyle);
        GUI.color = originalColor;

        if (isHovering) Repaint();
    }

    private void DrawDragOverlay()
    {
        if (!_isDragging || _draggedItem == null)
            return;
        if (_draggedItem?.asset == null)
            return;

        var mousePos = Event.current.mousePosition;
        var overlayRect = new Rect(mousePos.x - _dragOffset.x, mousePos.y - _dragOffset.y, 200, ITEM_HEIGHT);

        EditorGUI.DrawRect(overlayRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        GUI.Label(overlayRect, _draggedItem.asset.name, EditorStyles.whiteLabel);
        Repaint();
    }

    private void HandleExternalDragAndDrop()
    {
        if (_isDragging)
            return;

        var currentEvent = Event.current;

        if ((currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform) && DragAndDrop.objectReferences.Length > 0)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object obj in DragAndDrop.objectReferences)
                    AddAsset(obj);

                SaveDataToPrefs();
            }

            currentEvent.Use();
        }
    }

    private void HandleItemMouseEvents(AssetItem item, Rect itemRect)
    {
        var currentEvent = Event.current;
        if (!itemRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            _draggedItem = item;
            _dragOffset = currentEvent.mousePosition - itemRect.position;
            _isDragging = false;
            currentEvent.Use();
        }
    }

    private void HandleItemDragging()
    {
        var currentEvent = Event.current;
        if (_draggedItem == null)
            return;

        if (currentEvent.type == EventType.MouseDrag)
        {
            if (!_isDragging)
                StartDrag();

            Repaint();
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp)
        {
            if (_isDragging)
                EndDrag();
            else
                _draggedItem = null;

            Repaint();
            currentEvent.Use();
        }
    }

    private void HandleDropZoneDetection(Rect itemRect, string categoryName, int indexInCategory)
    {
        if (!_isDragging || _draggedItem == null || _draggedItem.asset == null)
            return;

        var mousePos = Event.current.mousePosition;
        var itemsInCategory = _assets.Where(f => f.categoryName == categoryName).ToList();
        bool isLastItem = indexInCategory == itemsInCategory.Count - 1;

        if (IsMouseInDropZone(mousePos, itemRect, true))
        {
            DrawDropZoneIndicator(itemRect, true);
            if (Event.current.type == EventType.MouseUp)
            {
                MoveItemToCategory(_draggedItem, categoryName, indexInCategory);
                Event.current.Use();
            }
        }
        else if (isLastItem && IsMouseInDropZone(mousePos, itemRect, false))
        {
            DrawDropZoneIndicator(itemRect, false);
            if (Event.current.type == EventType.MouseUp)
            {
                MoveItemToCategory(_draggedItem, categoryName, indexInCategory + 1);
                Event.current.Use();
            }
        }
    }

    private bool IsMouseInDropZone(Vector2 mousePos, Rect itemRect, bool above)
    {
        float targetY = above ? itemRect.y : itemRect.y + itemRect.height;

        return
            mousePos.y >= targetY - HOVER_THRESHOLD &&
            mousePos.y <= targetY + HOVER_THRESHOLD &&
            mousePos.x >= itemRect.x &&
            mousePos.x <= itemRect.x + itemRect.width;
    }

    private void DrawDropZoneIndicator(Rect itemRect, bool above)
    {
        float yPosition = above ? itemRect.y - DROP_ZONE_HEIGHT / 2 : itemRect.y + itemRect.height - DROP_ZONE_HEIGHT / 2;
        var dropZone = new Rect(itemRect.x, yPosition, itemRect.width, DROP_ZONE_HEIGHT);
        EditorGUI.DrawRect(dropZone, new Color(0.5f, 0.8f, 1f, 0.8f));
        Repaint();
    }

    private void StartDrag()
    {
        _isDragging = true;
        _dragStartTime = Time.realtimeSinceStartup;
        _autoExpandedCategories.Clear();
        _hoveredCategoryForExpand = null;
    }

    private void EndDrag()
    {
        _isDragging = false;
        _draggedItem = null;
        _hoveredCategoryForExpand = null;

        foreach (var categoryName in _autoExpandedCategories)
        {
            var category = _categories.FirstOrDefault(c => c.name == categoryName);
            if (category != null) category.isExpanded = false;
        }

        _autoExpandedCategories.Clear();
        SaveDataToPrefs();
    }

    private void HandleDragAutoExpand()
    {
        HandleGlobalAutoExpand();
        HandleHoverAutoExpand();
    }

    private void HandleGlobalAutoExpand()
    {
        if (!_isDragging || _draggedItem == null)
            return;
        if (Time.realtimeSinceStartup - _dragStartTime <= AUTO_EXPAND_DELAY)
            return;

        foreach (var category in _categories.Where(c => !c.isExpanded))
        {
            category.isExpanded = true;
            _autoExpandedCategories.Add(category.name);
        }
    }

    private void HandleHoverAutoExpand()
    {
        if (!_isDragging || _hoveredCategoryForExpand == null)
            return;
        if (Time.realtimeSinceStartup - _categoryHoverStartTime <= CATEGORY_HOVER_DELAY)
            return;

        var category = _categories.FirstOrDefault(c => c.name == _hoveredCategoryForExpand);
        if (category != null && !category.isExpanded)
        {
            category.isExpanded = true;
            _autoExpandedCategories.Add(category.name);
            SaveDataToPrefs();
        }
        _hoveredCategoryForExpand = null;
    }

    private void HandleCategoryHoverForAutoExpand(AssetCategory category)
    {
        if (!_isDragging || _draggedItem == null || category.isExpanded)
            return;

        var categoryHeaderRect = GUILayoutUtility.GetLastRect();
        bool isHovering = categoryHeaderRect.Contains(Event.current.mousePosition);

        if (isHovering && _hoveredCategoryForExpand != category.name)
        {
            _hoveredCategoryForExpand = category.name;
            _categoryHoverStartTime = Time.realtimeSinceStartup;
        }
        else if (!isHovering && _hoveredCategoryForExpand == category.name)
        {
            _hoveredCategoryForExpand = null;
        }
    }

    private void MoveItemToCategory(AssetItem item, string targetCategory, int insertIndex)
    {
        if (item == null)
            return;

        _assets.Remove(item);
        item.categoryName = targetCategory;

        var itemsInCategory = _assets.Where(f => f.categoryName == targetCategory).ToList();
        int insertPosition = CalculateInsertPosition(targetCategory, insertIndex, itemsInCategory.Count);

        if (insertPosition >= _assets.Count)
            _assets.Add(item);
        else
            _assets.Insert(insertPosition, item);

        SaveDataToPrefs();
        EndDrag();
    }

    private int CalculateInsertPosition(string targetCategory, int insertIndex, int categoryItemCount)
    {
        if (insertIndex <= 0)
        {
            var firstItem = _assets.FirstOrDefault(f => f.categoryName == targetCategory);
            return firstItem != null ? _assets.IndexOf(firstItem) : _assets.Count;
        }

        if (insertIndex >= categoryItemCount)
            return _assets.Count;

        var itemsInCategory = _assets.Where(f => f.categoryName == targetCategory).ToList();
        var targetItem = itemsInCategory[insertIndex - 1];
        return _assets.IndexOf(targetItem) + 1;
    }

    private void ShowCategoryContextMenu(AssetCategory category)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Rename"), false, () => StartRename(category));
        menu.AddItem(new GUIContent("Clear Category"), false, () => ShowClearCategoryDialog(category));
        menu.AddItem(new GUIContent("Delete Category"), false, () => ShowDeleteCategoryDialog(category));
        menu.ShowAsContext();
    }

    private void StartRename(AssetCategory category)
    {
        _categoryBeingRenamed = category;
        _renameBuffer = category.name;
    }

    private void ConfirmRename(AssetCategory category)
    {
        if (!string.IsNullOrEmpty(_renameBuffer) && _renameBuffer != category.name)
        {
            foreach (var item in _assets.Where(f => f.categoryName == category.name))
                item.categoryName = _renameBuffer;

            category.name = _renameBuffer;
            SaveDataToPrefs();
        }
        CancelRename();
    }

    private void CancelRename()
    {
        _categoryBeingRenamed = null;
        _renameBuffer = "";
    }

    private void ShowDeleteCategoryDialog(AssetCategory category)
    {
        string title = "Delete Category";
        string message = $"What would you like to do with category '{category.name}'?";
        string firstOption = "Delete all (category + assets)";
        string secondOption = "Uncategorize assets and delete category";
        string cancel = "Cancel";

        int choice = EditorUtility.DisplayDialogComplex(title, message, firstOption, secondOption, cancel);
        switch (choice)
        {
            case 0:
                _assets.RemoveAll(f => f.categoryName == category.name);
                _categories.Remove(category);
                SaveDataToPrefs();
                break;
            case 1:
                foreach (var item in _assets.Where(f => f.categoryName == category.name))
                    item.categoryName = "";
                _categories.Remove(category);
                SaveDataToPrefs();
                break;
        }
    }

    private void ShowClearCategoryDialog(AssetCategory category)
    {
        string title = "Clear Category";
        string message = $"What would you like to do with the contents of '{category.name}'?";
        string firstOption = "Remove all assets";
        string secondOption = "Uncategorize assets";
        string cancel = "Cancel";

        int choice = EditorUtility.DisplayDialogComplex(title, message, firstOption, secondOption, cancel);
        switch (choice)
        {
            case 0:
                _assets.RemoveAll(f => f.categoryName == category.name);
                SaveDataToPrefs();
                break;
            case 1:
                foreach (var item in _assets.Where(f => f.categoryName == category.name))
                    item.categoryName = "";
                SaveDataToPrefs();
                break;
        }
    }

    private void ConfirmRemoveAllAssets()
    {
        string title = "Remove All Assets";
        string message = "Are you sure you want to remove all assets?\n\nThis action is irreversible.";
        string confirm = "Confirm";
        string cancel = "Cancel";

        if (EditorUtility.DisplayDialog(title, message, confirm, cancel))
        {
            _assets.Clear();
            SaveDataToPrefs();
        }
    }

    private void ConfirmRemoveEverything()
    {
        string title = "Remove All";
        string message = "Are you sure you want to remove all categories and assets?\n\nThis action is irreversible.";
        string confirm = "Confirm";
        string cancel = "Cancel";

        if (EditorUtility.DisplayDialog(title, message, confirm, cancel))
        {
            _assets.Clear();
            _categories.Clear();
            SaveDataToPrefs();
        }
    }

    private void AddAsset(Object obj, string categoryName = "")
    {
        string path = AssetDatabase.GetAssetPath(obj);

        if (string.IsNullOrEmpty(path))
            return;

        if (AssetDatabase.IsSubAsset(obj))
            return;

        string newGuid = AssetDatabase.AssetPathToGUID(path);

        if (_assets.Any(f => f.guid == newGuid))
            return;

        var item = new AssetItem(obj, categoryName);
        _assets.Add(item);

        if (!string.IsNullOrEmpty(categoryName) && !_categories.Any(c => c.name == categoryName))
            AddCategory(categoryName);

        SaveDataToPrefs();
    }

    private void AddCategory(string name)
    {
        if (_categories.Any(c => c.name == name))
            return;

        _categories.Add(new AssetCategory(name));
        SaveDataToPrefs();
    }

    private void SaveDataToPrefs()
    {
        var assetJson = JsonUtility.ToJson(new SerializableList<AssetItem>(_assets), true);
        var categoriesJson = JsonUtility.ToJson(new SerializableList<AssetCategory>(_categories), true);

        EditorPrefs.SetString(ITEMS_KEY, assetJson);
        EditorPrefs.SetString(CATEGORIES_KEY, categoriesJson);
        EditorPrefs.SetFloat(SLIDER_KEY, _sliderCurrentValue);
        EditorPrefs.SetBool(FOLDOUT_KEY, _showFoldersDropdown);
    }

    private void LoadDataFromPrefs()
    {
        var json = EditorPrefs.GetString(ITEMS_KEY, "");

        if (string.IsNullOrEmpty(json))
            _assets = new List<AssetItem>();
        else
        {
            var loaded = JsonUtility.FromJson<SerializableList<AssetItem>>(json)?.items ?? new List<AssetItem>();

            _assets = loaded
                .Where(f => !string.IsNullOrEmpty(f.guid))
                .GroupBy(f => f.guid)
                .Select(g => g.First())
                .ToList();
        }

        foreach (var f in _assets)
            f.RestoreAssetReference();

        _assets.RemoveAll(f => f.asset == null);

        var catJson = EditorPrefs.GetString(CATEGORIES_KEY, "");
        _categories = string.IsNullOrEmpty(catJson) ? new List<AssetCategory>() : JsonUtility.FromJson<SerializableList<AssetCategory>>(catJson)?.items ?? new List<AssetCategory>();

        _sliderCurrentValue = EditorPrefs.GetFloat(SLIDER_KEY, 1);
        _showFoldersDropdown = EditorPrefs.GetBool(FOLDOUT_KEY, false);
    }

    private void RefreshAllAssetReferences()
    {
        foreach (var f in _assets)
            f.RestoreAssetReference();

        SaveDataToPrefs();
        Repaint();
    }

    [System.Serializable]
    public class AssetItem
    {
        public Object asset;
        public string categoryName = "";
        public string guid;

        public AssetItem(Object obj, string category = "")
        {
            asset = obj;
            categoryName = category;

            if (obj != null)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                guid = AssetDatabase.AssetPathToGUID(path);
            }
        }

        public void RestoreAssetReference()
        {
            if (string.IsNullOrEmpty(guid))
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                asset = null;
                return;
            }

            var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            else
                asset = AssetDatabase.LoadAssetAtPath(path, mainType);
        }
    }

    [System.Serializable]
    public class AssetCategory
    {
        public string name;
        public bool isExpanded = true;
        public Color color = Color.white;

        public AssetCategory(string categoryName)
        {
            name = categoryName;
        }
    }

    [System.Serializable]
    public class SerializableList<T>
    {
        public List<T> items;
        public SerializableList(List<T> list) => items = list;
    }
}