using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using static TileGameCore.List.CustomField;

namespace TileGameCore.List
{
    public class CustomList
    {
        //data
        private SerializedObject serializedObject;
        private SerializedProperty elementsProperty;
        private List<SerializedProperty> elementsList;
        private bool usingListInterface = false;
        private IList elements;

        private int minWidth = 150;

        //global

        private int selectedIndex = -1;
        private readonly HashSet<int> selectedIndices = new HashSet<int>();
        private int selectionAnchor = -1;
        private int prevIndent;
        
        private Event currentEvent;
        private bool executedOnce;

        private bool usingPropertyList;
        private bool stretchHeight = true;
        private bool stretchWidth = true;
        public CustomListStyle style;
        private GUIStyle controlStyle;

        private Rect globalRect;
        private Rect footerPaginationRect;
        private Rect footerButtonsRect;
        private Rect listRect;
        private Rect filledElementsRect;


        //header
        private Rect headerRect;
        private Rect headerContentRect;

        //footer
        private float rightEdge;
        private float leftEdge;
        private Rect buttonsRect;
        private Rect footerButtonRect;

        //list & drag
        private bool dragging;
        private bool isSelected;
        private int fullIndex;
        private int startDragIndex;
        private int currentDragIndex;
        private int dragIndexAdjustment;
        private bool draggingMulti;
        private List<int> dragIndices;
        // Reused while multi-dragging so DrawListWhileDragging does not allocate every frame.
        private List<int> dragRemainingIndices;
        private Vector2 dragLockedScrollPosition;
        private int maxElementCount;
        private float tempX;
        private float tempY;
        float draggedElementY;
        private float previousDraggedElementY;

        private float dragOffset;
        private bool isDraggedElementExpanded;
        private float draggedElemenentHeight;
        private float mouseYinList;
        private Rect listContentRect;
        private Rect elementRect;
        private Rect draggingHandleRect;
        private Rect elementHeaderRect;

        private Vector2 lastMouseDownPosition;
        private int lastMouseDownIndex;
        private float lastMouseDownHeight;
        // Last completed reorder (single-item from/to). -1 from means multi/unknown.
        public int LastReorderFrom { get; private set; } = -1;
        public int LastReorderTo { get; private set; } = -1;
        private float focusedElementHeight;
        private SerializedProperty focusedElementProperty;
        private int currenElementCount;
        private int tempElementCount;
        private int availableSlots;
        private float elementHeaderRectOffsetY;
        private float draggingHandleRectOffsetY;
        private const float RowNumberWidth = 40f;
        private Rect rowNumberRect;
        private float rowNumberRectOffsetY;
        private int visualRowIndex = -1;
        private GUIStyle rowNumberStyle;
        private float foldoutRectOffsetY;
        private float fieldRectOffsetY;
        private float[] fieldHeights;
        private float minPosiibleHeight;
        private float desiredHeight;
        private float restOfthelistHeight;

        //pagination
        private const int DefaultPageSize = 40;
        private const int MinPageSize = 1;
        private const int MaxPageSize = 500;
        private int pageSize = DefaultPageSize;
        private int viewStartIndex;
        private int viewEndInclusive = int.MaxValue;
        private bool hasViewRange;
        private int[] displayedIndices;
        private bool enablePagination;
        private int currentPage;
        private int pagesCount;
        private int pageBeginIndex;
        private int currentBeginIndex;
        private int focusedElementIndex;
        private int pageElementCount;
        private int lastPageSyncSelectedIndex = int.MinValue;
        private bool selectionScrollPending;
        private Vector2 listScrollPosition;
        private Rect paginationContentRect;
        private Rect firstPageButtonRect;
        private Rect previousPageButtonRect;
        private Rect lastPageButtonRect;
        private Rect paginationLabelRect;
        private Rect paginationPageFieldRect;
        private Rect paginationTotalLabelRect;
        private Rect nextPageButtonRect;

       


        #region delegates
        public delegate string GetHeaderLabelCallbackDelegate();
        public delegate void SelectionChangedCallbackDelegate();
        public delegate void ListChangedCallbackDelegate();
        public delegate void ListReorderedCallbackDelegate();
        public delegate void ListReorderedCallbackWithDetailsDelegate(int srcIndex, int dstIndex);
        public delegate void AddElementCallbackDelegate();
        public delegate void AddElementWithDropdownCallback();
        public delegate void RemoveElementCallbackDelegate();
        public delegate void DisplayContextMenuCallbackDelegate();
        public delegate void ListUndoCallbackDelegate();

        public GetHeaderLabelCallbackDelegate getHeaderLabelCallback;
        public SelectionChangedCallbackDelegate selectionChangedCallback;
        public ListReorderedCallbackDelegate listReorderedCallback;
        public ListReorderedCallbackWithDetailsDelegate listReorderedCallbackWithDetails;
        public ListChangedCallbackDelegate listChangedCallback;
        public AddElementCallbackDelegate addElementCallback;
        public AddElementWithDropdownCallback addElementWithDropdownCallback;
        public RemoveElementCallbackDelegate removeElementCallback;
        public DisplayContextMenuCallbackDelegate displayContextMenuCallback;
        public ListUndoCallbackDelegate listUndoCallback;
        #endregion



        //element
        bool useFoldout;
        bool useLabelProperty;
        public delegate string GetLabelDelegate(SerializedProperty elementProperty, int elementIndex);
        private GetLabelDelegate getLabelCallback;
        private List<AbstractField> fields;
        private string labelPropertyName;
        private SerializedProperty currentElementProperty;
        private Rect bodyRect;
        private Rect labelRect;
        private Rect removeButtonRect;
        private Rect headerButtonRect;
        private Rect backgroundRect;
        private Rect calculatedGlobalRect;
        private Rect foldoutRect;
        private Rect fieldRect;
        private float bodyHeight;
        private bool ignoreDragEvents;
        private bool ignoreKeyboardArrows;
        private bool checkboxSelectionMode;
        private Rect checkboxRect;

        private float CollapsedElementHeight => style.element.collapsedElementHeight;

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value < 0)
                {
                    ClearMultiSelection();
                    selectedIndex = -1;
                    return;
                }

                SetSingleSelection(value, invokeCallback: false);
            }
        }

        public IReadOnlyCollection<int> SelectedIndices => selectedIndices;
        public int SelectedCount => selectedIndices.Count;

        public int MinWidth { get => minWidth; set => minWidth = value; }
        public bool StretchHeight { get => stretchHeight; set => stretchHeight = value; }
        public bool StretchWidth { get => stretchWidth; set => stretchWidth = value; }
        public bool IgnoreDragEvents { get => ignoreDragEvents; set => ignoreDragEvents = value; }
        public bool IgnoreKeyboardArrows { get => ignoreKeyboardArrows; set => ignoreKeyboardArrows = value; }
        public bool CheckboxSelectionMode { get => checkboxSelectionMode; set => checkboxSelectionMode = value; }

        public int PageSize
        {
            get => pageSize;
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == pageSize)
                {
                    return;
                }

                pageSize = clamped;
                listScrollPosition = Vector2.zero;
                // Force DoCalculations to rebuild list/pagination layout for the new page size.
                calculatedGlobalRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);

                if (selectedIndex >= 0)
                {
                    currentPage = PageForIndex(selectedIndex);
                    lastPageSyncSelectedIndex = selectedIndex;
                    selectionScrollPending = true;
                }
                else
                {
                    currentPage = 0;
                    lastPageSyncSelectedIndex = selectedIndex;
                }

                if (EditorWindow.focusedWindow != null)
                {
                    EditorWindow.focusedWindow.Repaint();
                }
            }
        }

        public bool IsSelected(int index)
        {
            return selectedIndices.Contains(index);
        }

        public void ClearMultiSelection()
        {
            selectedIndices.Clear();
            selectionAnchor = -1;
            selectedIndex = -1;
        }

        public void SetSingleSelection(int index, bool invokeCallback = true)
        {
            selectedIndices.Clear();
            if (index < 0 || index >= ArraySize())
            {
                selectedIndex = -1;
                selectionAnchor = -1;
                return;
            }

            selectedIndex = index;
            selectionAnchor = index;
            selectedIndices.Add(index);

            if (invokeCallback)
            {
                OnSelectionChanged(index);
            }
        }

        public void ApplySelectionRange(int from, int to)
        {
            if (ArraySize() <= 0)
            {
                ClearMultiSelection();
                return;
            }

            SelectRange(from, to);
            selectedIndex = Mathf.Clamp(to, 0, ArraySize() - 1);
            selectionAnchor = Mathf.Clamp(from, 0, ArraySize() - 1);
            CollapseElements();
        }

        public List<int> GetSortedSelectedIndices()
        {
            List<int> sorted = selectedIndices.ToList();
            sorted.Sort();
            return sorted;
        }

        private void SelectRange(int from, int to)
        {
            selectedIndices.Clear();
            int min = Mathf.Min(from, to);
            int max = Mathf.Max(from, to);
            int size = ArraySize();
            min = Mathf.Clamp(min, 0, size - 1);
            max = Mathf.Clamp(max, 0, size - 1);
            for (int i = min; i <= max; i++)
            {
                selectedIndices.Add(i);
            }
        }

        private void HandleRowSelectionClick(int index, int mouseButton)
        {
            EditorGUIUtility.keyboardControl = -1;

            if (mouseButton == 1)
            {
                // Right-click: if not already in multi-selection, select only this row.
                if (!selectedIndices.Contains(index))
                {
                    SetSingleSelection(index, invokeCallback: true);
                }
                else
                {
                    selectedIndex = index;
                    selectionChangedCallback?.Invoke();
                }

                DisplayContextMenu();
                return;
            }

            if (mouseButton != 0)
            {
                return;
            }

            bool ctrl = currentEvent.control || currentEvent.command;
            bool shift = currentEvent.shift;

            if (ctrl)
            {
                ToggleMultiSelection(index);
                return;
            }

            if (shift && selectionAnchor >= 0 && ArraySize() > 0)
            {
                SelectRange(selectionAnchor, index);
                selectedIndex = index;
                CollapseElements();
                selectionChangedCallback?.Invoke();
                return;
            }

            SetSingleSelection(index, invokeCallback: true);
        }

        private void ToggleMultiSelection(int index)
        {
            if (index < 0 || index >= ArraySize())
            {
                return;
            }

            if (selectedIndices.Contains(index))
            {
                selectedIndices.Remove(index);
                if (selectedIndices.Count == 0)
                {
                    selectedIndex = -1;
                    selectionAnchor = -1;
                    selectionChangedCallback?.Invoke();
                    return;
                }

                if (selectedIndex == index)
                {
                    selectedIndex = GetSortedSelectedIndices()[0];
                }
            }
            else
            {
                selectedIndices.Add(index);
                selectedIndex = index;
                if (selectionAnchor < 0)
                {
                    selectionAnchor = index;
                }
            }

            CollapseElements();
            selectionChangedCallback?.Invoke();
        }

        /// <summary>
        /// Activate a row for preview without clearing other checked items (checkbox mode).
        /// </summary>
        private void ActivateWithoutClearingOthers(int index)
        {
            if (index < 0 || index >= ArraySize())
            {
                return;
            }

            if (!selectedIndices.Contains(index))
            {
                selectedIndices.Add(index);
            }

            selectedIndex = index;
            if (selectionAnchor < 0)
            {
                selectionAnchor = index;
            }

            CollapseElements();
            selectionChangedCallback?.Invoke();
        }

        public CustomList(SerializedObject serializedObject, SerializedProperty elements, string labelPropertyName)
        {
            this.serializedObject = serializedObject;
            this.labelPropertyName = labelPropertyName;
            useLabelProperty = true;
            useFoldout = false;
            elementsProperty = elements;
            usingPropertyList = false;
            LoadStyle();
        }

        public CustomList(SerializedObject serializedObject, SerializedProperty elements, GetLabelDelegate getLabelCallback)
        {
            this.serializedObject = serializedObject;
            this.getLabelCallback = getLabelCallback;
            useLabelProperty = false;
            useFoldout = false;
            elementsProperty = elements;
            usingPropertyList = false;
            LoadStyle();
        }

        public CustomList(SerializedObject serializedObject, List<SerializedProperty> propertyList, string labelPropertyName)
        {
            this.serializedObject = serializedObject;
            this.labelPropertyName = labelPropertyName;
            useLabelProperty = true;
            useFoldout = false;
            elementsList = propertyList;
            usingPropertyList = true;
            LoadStyle();
        }

        public CustomList(SerializedObject serializedObject, List<SerializedProperty> propertyList, GetLabelDelegate getLabelCallback)
        {
            this.serializedObject = serializedObject;
            this.getLabelCallback = getLabelCallback;
            useLabelProperty = false;
            useFoldout = false;
            elementsList = propertyList;
            usingPropertyList = true;
            LoadStyle();
        }

        public CustomList(IList elements, GetLabelDelegate getLabelCallback)
        {
            this.getLabelCallback = getLabelCallback;
            useLabelProperty = false;
            useFoldout = false;
            this.elements = elements;
            usingListInterface = true;
            LoadStyle();
        }



        public void LoadStyle(int index = 0)
        {
            ListStylesDatabase listStylesDatabase = EditorUtils.GetAsset<ListStylesDatabase>();

            if(listStylesDatabase == null)
            {
                style = new CustomListStyle();
                style.SetDefaultStyleValues();
            }
            else
            {
                style = listStylesDatabase.GetStyle(index);
            }
        }

        public void AddPropertyField(string propertyName)
        {
            if(fields == null)
            {
                fields = new List<AbstractField>();
                useFoldout = true;
            }

            fields.Add(new PropertyField(propertyName));
        }

        public void AddPropertyField(string propertyName, GUIContent customGUIContent)
        {
            if (fields == null)
            {
                fields = new List<AbstractField>();
                useFoldout = true;
            }

            fields.Add(new PropertyField(propertyName, customGUIContent));
        }

        public void AddCustomField(DrawCallbackDelegate drawCallbackDelegate, GetHeightCallbackDelegate getHeightCallbackDelegate)
        {
            if (fields == null)
            {
                fields = new List<AbstractField>();
                useFoldout = true;
            }

            fields.Add(new CustomField(drawCallbackDelegate, getHeightCallbackDelegate));
        }

        public void AddSpace()
        {
            if (fields == null)
            {
                fields = new List<AbstractField>();
                useFoldout = true;
            }

            fields.Add(new Space());
        }

        public void AddSpace(float height)
        {
            if (fields == null)
            {
                fields = new List<AbstractField>();
                useFoldout = true;
            }

            fields.Add(new Space(height));
        }

        public void AddSeparator()
        {
            if (fields == null)
            {
                fields = new List<AbstractField>();
                useFoldout = true;
            }

            fields.Add(new Separator());
        }

        public void AddSeparator(Color color)
        {
            if (fields == null)
            {
                fields = new List<AbstractField>();
                useFoldout = true;
            }

            fields.Add(new Separator(color));
        }

        #region Data

        public bool IsExpanded(int index)
        {
            if (usingListInterface)
            {
                return false;
            }
            else
            {
                return GetElement(index).isExpanded;
            }
        }

        public void SetIsExpanded(int index, bool value)
        {
            if (usingListInterface)
            {
                return;
            }

            GetElement(index).isExpanded = value;
        }

        public SerializedProperty GetElement(int index)
        {
            if (usingListInterface)
            {
                return null;
            }

            if(index >= ArraySize() || (index < 0))
            {
                Debug.LogError("Retrieving index out of bounds:" + index);
            }

            if (usingPropertyList)
            {
                return elementsList[index];
            }
            else
            {
                return elementsProperty.GetArrayElementAtIndex(index);
            }
        }

        public int ArraySize()
        {
            if (usingListInterface)
            {
                return elements.Count;
            }
            else if (usingPropertyList)
            {
                return elementsList.Count;
            }
            else
            {
                return elementsProperty.arraySize;
            }
        }

        public void SetViewRange(int startInclusive, int endInclusive)
        {
            if (hasViewRange && viewStartIndex == startInclusive && viewEndInclusive == endInclusive)
            {
                return;
            }

            hasViewRange = true;
            viewStartIndex = startInclusive;
            viewEndInclusive = endInclusive;
            currentPage = 0;
            lastPageSyncSelectedIndex = int.MinValue;
            listScrollPosition = Vector2.zero;
            calculatedGlobalRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
        }

        public void SetDisplayedIndices(int[] indices)
        {
            if (SameIndices(displayedIndices, indices))
            {
                return;
            }

            displayedIndices = indices;
            CancelDragging();
            currentPage = 0;
            lastPageSyncSelectedIndex = int.MinValue;
            listScrollPosition = Vector2.zero;
            calculatedGlobalRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
        }

        private static bool SameIndices(int[] a, int[] b)
        {
            if (a == b)
            {
                return true;
            }

            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool Filtering => displayedIndices != null;

        private int RealIndex(int slot)
        {
            if (!Filtering || slot < 0 || slot >= displayedIndices.Length)
            {
                return slot;
            }

            return displayedIndices[slot];
        }

        private int SlotOf(int realIndex)
        {
            if (!Filtering)
            {
                return realIndex;
            }

            for (int i = 0; i < displayedIndices.Length; i++)
            {
                if (displayedIndices[i] == realIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FocusRealIndex(int slotStart, int slotEndExclusive)
        {
            if (slotEndExclusive <= slotStart)
            {
                return 0;
            }

            if (!Filtering)
            {
                return Mathf.Clamp(selectedIndex, slotStart, slotEndExclusive - 1);
            }

            int slot = SlotOf(selectedIndex);
            if (slot >= slotStart && slot < slotEndExclusive)
            {
                return selectedIndex;
            }

            return RealIndex(slotStart);
        }

        private int ViewStart()
        {
            if (Filtering)
            {
                return 0;
            }

            int size = ArraySize();
            if (size <= 0)
            {
                return 0;
            }

            if (!hasViewRange)
            {
                return 0;
            }

            return Mathf.Clamp(viewStartIndex, 0, size - 1);
        }

        private int ViewEndExclusive()
        {
            if (Filtering)
            {
                return displayedIndices.Length;
            }

            int size = ArraySize();
            if (size <= 0)
            {
                return 0;
            }

            if (!hasViewRange)
            {
                return size;
            }

            return Mathf.Clamp(viewEndInclusive, ViewStart(), size - 1) + 1;
        }

        private int ViewCount()
        {
            return Mathf.Max(0, ViewEndExclusive() - ViewStart());
        }

        private int PageForIndex(int index)
        {
            int count = pageElementCount > 0 ? pageElementCount : pageSize;
            if (count <= 0)
            {
                return 0;
            }

            if (!Filtering)
            {
                return Mathf.Max(0, (index - ViewStart()) / count);
            }

            int slot = SlotOf(index);
            return slot < 0 ? 0 : Mathf.Max(0, slot / count);
        }

        public void MoveElement(int srcIndex,int destIndex)
        {
            if(listReorderedCallbackWithDetails != null)
            {
                listReorderedCallbackWithDetails.Invoke(srcIndex, destIndex);
            }
            else
            {
                if (usingListInterface)
                {
                    UndoCallback();
                    var item = elements[srcIndex];
                    elements.RemoveAt(srcIndex);
                    elements.Insert(destIndex, item);

                }
                else if (usingPropertyList)
                {
                    UndoCallback();
                    SerializedProperty temp = elementsList[srcIndex];
                    elementsList.RemoveAt(srcIndex);
                    elementsList.Insert(destIndex, temp);
                }
                else
                {
                    // Reorder is pointer-only and intentionally not undoable (no CompleteObjectUndo).
                    elementsProperty.MoveArrayElement(srcIndex, destIndex);
                    ApplyElementsPropertyChangesWithoutUndo();
                }
            }

            LastReorderFrom = srcIndex;
            LastReorderTo = destIndex;
            listReorderedCallback?.Invoke();
            ListChangedCallback();
        }

        private void ApplyElementsPropertyChangesWithoutUndo()
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            if (serializedObject.targetObject != null)
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

        #endregion



        public void Display()
        {
            ExecuteStuffOnce();

            currentEvent = Event.current;
            prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            HandleKeyboardArrows();

            DoCalculations();
            HandleDrawingBackgroundConfiguration(globalRect, style.globalBackground);

            

            if (style.enableHeader)
            {
                DrawHeader();
            }

            // Keep in-progress drag state fresh before painting (MouseUp/Move must not be missed).
            if (dragging)
            {
                HandleDraggingDetection();
            }

            DrawList();

            if (enablePagination)
            {
                DrawPagination();
            }

            if (style.enableFooterAddButton || style.enableFooterRemoveButton)
            {
                DrawFooterButtons();
            }

            

            EditorGUI.indentLevel = prevIndent;

            // Drag start depends on lastMouseDownIndex written inside DrawElement.
            if (!dragging)
            {
                HandleDraggingDetection();
            }
        }

        private void HandleKeyboardArrows()
        {

            if((Event.current.type != EventType.KeyDown) || IgnoreKeyboardArrows || (ViewCount() == 0))
            {
                return;
            }

            int viewStart = ViewStart();
            int viewLast = ViewEndExclusive() - 1;
            int slot = Filtering ? SlotOf(selectedIndex) : selectedIndex;
            if (slot < 0)
            {
                slot = viewStart;
            }

            if (Event.current.keyCode == KeyCode.DownArrow)
            {
                slot = Mathf.Clamp(slot + 1, viewStart, viewLast);
            }
            else if (Event.current.keyCode == KeyCode.UpArrow)
            {
                slot = Mathf.Clamp(slot - 1, viewStart, viewLast);
            }
            else if (Event.current.keyCode == KeyCode.RightArrow)
            {
                slot = Mathf.Clamp(slot + pageElementCount, viewStart, viewLast);
            }
            else if (Event.current.keyCode == KeyCode.LeftArrow)
            {
                slot = Mathf.Clamp(slot - pageElementCount, viewStart, viewLast);
            }
            else
            {
                return;
            }

            int nextIndex = RealIndex(slot);

            if (Event.current.shift && selectionAnchor >= 0)
            {
                SelectRange(selectionAnchor, nextIndex);
                selectedIndex = nextIndex;
                CollapseElements();
                selectionChangedCallback?.Invoke();
            }
            else
            {
                SetSingleSelection(nextIndex, invokeCallback: true);
            }

            Event.current.Use();
        }

        private void ExecuteStuffOnce()
        {
            if (!executedOnce)  //stuff that called once
            {
                executedOnce = true;
                CollapseElements();

                if (useFoldout)
                {
                    fieldHeights = new float[fields.Count];
                }
                

                //style for control
                controlStyle = new GUIStyle();
                controlStyle.stretchHeight = stretchHeight;
                controlStyle.stretchWidth = stretchWidth;


                //calculating prefered height
                restOfthelistHeight = 0;

                if (style.enableHeader)
                {
                    restOfthelistHeight += style.header.height;
                }

                restOfthelistHeight += style.footerButtons.height;
                restOfthelistHeight += style.list.contentPaddingBottom;
                restOfthelistHeight += style.list.contentPaddingTop;

                //init rects
                listRect = new Rect();
                headerRect = new Rect();
                footerButtonsRect = new Rect();
                listContentRect = new Rect();
                footerPaginationRect = new Rect();
                filledElementsRect = new Rect();
                backgroundRect = new Rect();
                calculatedGlobalRect = new Rect();
                elementHeaderRect = new Rect();
                globalRect = new Rect();
                footerPaginationRect = new Rect();
                footerButtonsRect = new Rect();
                headerContentRect = new Rect();
                buttonsRect = new Rect();
                footerButtonRect = new Rect();
                elementRect = new Rect();
                draggingHandleRect = new Rect();
                paginationContentRect = new Rect();
                firstPageButtonRect = new Rect();
                previousPageButtonRect = new Rect();
                lastPageButtonRect = new Rect();
                paginationLabelRect = new Rect();
                paginationPageFieldRect = new Rect();
                paginationTotalLabelRect = new Rect();
                nextPageButtonRect = new Rect();
                bodyRect = new Rect();
                labelRect = new Rect();
                removeButtonRect = new Rect();
                headerButtonRect = new Rect();
                foldoutRect = new Rect();
                fieldRect = new Rect();
            }
        }

        


        private void DoCalculations()
        {
            //Attempt to minimize the list
            int viewCount = ViewCount();
            if(viewCount > 0)
            {
                focusedElementIndex = FocusRealIndex(ViewStart(), ViewEndExclusive());
                CalculateFocusedElementHeight();

                if(focusedElementHeight > CollapsedElementHeight)
                {
                    minPosiibleHeight = focusedElementHeight + restOfthelistHeight;
                    desiredHeight = (viewCount - 1) * CollapsedElementHeight + minPosiibleHeight;
                }
                else
                {
                    minPosiibleHeight = CollapsedElementHeight + style.pagination.height + restOfthelistHeight;
                    desiredHeight = viewCount * CollapsedElementHeight + restOfthelistHeight;
                }
            }
            else
            {
                focusedElementHeight = 20f;//For a label;
                minPosiibleHeight = focusedElementHeight + restOfthelistHeight;
                desiredHeight = minPosiibleHeight;
            }

            globalRect = GUILayoutUtility.GetRect(GUIContent.none, controlStyle, GUILayout.MinHeight(minPosiibleHeight), GUILayout.MinWidth(MinWidth), GUILayout.MaxHeight(desiredHeight));

            if(globalRect.height < 5) // I don`t remember if it`s zero or one in some cases so I put 5
            {
                return;
            }

            if(!((currentEvent.type == EventType.Layout) || (currentEvent.type == EventType.Repaint)))
            {
                return;
            }

            int previousPageElementCount = pageElementCount;
            bool previousEnablePagination = enablePagination;
            pageElementCount = pageSize;
            enablePagination = ViewCount() > pageSize;
            bool paginationLayoutChanged = previousPageElementCount != pageElementCount
                || previousEnablePagination != enablePagination;

            if (calculatedGlobalRect != globalRect || paginationLayoutChanged) // rebuild when size or page size changes
            {
                listRect.Set(globalRect.x, globalRect.y, globalRect.width, globalRect.height);
                calculatedGlobalRect.Set(globalRect.x, globalRect.y, globalRect.width, globalRect.height);


                if (style.enableHeader)
                {
                    headerRect.Set(globalRect.x, globalRect.y, globalRect.width, style.header.height);

                    headerContentRect.x = style.header.contentPaddingLeft + headerRect.x;
                    headerContentRect.y = style.header.contentPaddingTop + headerRect.y;
                    headerContentRect.xMax = headerRect.xMax - style.header.contentPaddingRight;
                    headerContentRect.yMax = headerRect.yMax - style.header.contentPaddingBottom;
                    listRect.yMin += style.header.height;
                }

                //add footer
                footerButtonsRect.Set(globalRect.x, globalRect.y, globalRect.width, globalRect.height);
                footerButtonsRect.yMin = footerButtonsRect.yMax - style.footerButtons.height;
                listRect.yMax -= style.footerButtons.height;




                //max number of elements with pagination (if all of them isn`t expadned)
                listContentRect.x = style.list.contentPaddingLeft + listRect.x;
                listContentRect.y = style.list.contentPaddingTop + listRect.y;
                listContentRect.xMax = listRect.xMax - style.list.contentPaddingRight;
                listContentRect.yMax = listRect.yMax - style.list.contentPaddingBottom;

                maxElementCount = pageSize;


                //add space for pagination
                if (enablePagination)
                {
                    footerPaginationRect.Set(globalRect.position.x, footerButtonsRect.position.y - style.pagination.height, globalRect.width, style.pagination.height);
                    listRect.yMax -= style.pagination.height;
                    listContentRect.yMax -= style.pagination.height;

                    //Calculate other rects for pagination
                    paginationContentRect.x = style.pagination.contentPaddingLeft + footerPaginationRect.x;
                    paginationContentRect.y = style.pagination.contentPaddingTop + footerPaginationRect.y;
                    paginationContentRect.xMax = footerPaginationRect.xMax - style.pagination.contentPaddingRight;
                    paginationContentRect.yMax = footerPaginationRect.yMax - style.pagination.contentPaddingBottom;


                    firstPageButtonRect.Set(paginationContentRect.xMin, paginationContentRect.y, style.pagination.buttonsWidth, style.pagination.buttonsHeight);
                    previousPageButtonRect.Set(firstPageButtonRect.xMax, paginationContentRect.y, style.pagination.buttonsWidth, style.pagination.buttonsHeight);

                    nextPageButtonRect.Set(paginationContentRect.xMax - (2 * style.pagination.buttonsWidth), paginationContentRect.y, style.pagination.buttonsWidth, style.pagination.buttonsHeight);
                    lastPageButtonRect.Set(paginationContentRect.xMax - style.pagination.buttonsWidth, paginationContentRect.y, style.pagination.buttonsWidth, style.pagination.buttonsHeight);
                    paginationLabelRect.Set(previousPageButtonRect.xMax, paginationContentRect.y, nextPageButtonRect.xMin - previousPageButtonRect.xMax, style.pagination.labelHeight);

                    // Same Y/height as << < > >> so the page controls share one horizontal center line.
                    float pageFieldWidth = 34f;
                    float totalLabelWidth = 36f;
                    float pageGroupWidth = pageFieldWidth + totalLabelWidth;
                    float pageGroupX = paginationLabelRect.x + (paginationLabelRect.width - pageGroupWidth) * 0.5f;
                    paginationPageFieldRect.Set(pageGroupX, firstPageButtonRect.y, pageFieldWidth, firstPageButtonRect.height);
                    paginationTotalLabelRect.Set(paginationPageFieldRect.xMax, firstPageButtonRect.y, totalLabelWidth, firstPageButtonRect.height);
                }


                

                // Row numbers sit in a gutter. The draggable row starts to their right.
                float rowLeft = style.element.headerPaddingLeft + listContentRect.x;
                elementHeaderRect.x = rowLeft;
                elementHeaderRect.y = style.element.headerPaddingTop + listContentRect.y;
                elementHeaderRect.xMax = listContentRect.xMax - style.element.headerPaddingRight;
                elementHeaderRect.height = CollapsedElementHeight - style.element.headerPaddingBottom;

                rowNumberRect.Set(rowLeft, elementHeaderRect.y, RowNumberWidth - 4f, elementHeaderRect.height);
                elementHeaderRect.xMin += RowNumberWidth;

                draggingHandleRect.Set(elementHeaderRect.x + style.dragHandle.paddingLeft, elementHeaderRect.y, style.dragHandle.width, style.dragHandle.height);
                draggingHandleRect.y = elementHeaderRect.y + elementHeaderRect.height - style.dragHandle.paddingBottom - style.dragHandle.height;

                labelRect.Set(elementHeaderRect.x, elementHeaderRect.y, elementHeaderRect.width, elementHeaderRect.height);
                labelRect.xMin += style.dragHandle.allocatedHorizontalSpace;

                headerButtonRect.Set(elementHeaderRect.x, elementHeaderRect.y, elementHeaderRect.width, elementHeaderRect.height);

                if (useFoldout)
                {
                    foldoutRect.Set(elementHeaderRect.x + style.foldout.paddingLeft + style.dragHandle.allocatedHorizontalSpace, elementHeaderRect.y, style.foldout.width, style.foldout.height);
                    foldoutRect.y = elementHeaderRect.y + elementHeaderRect.height - style.foldout.paddingBottom - style.foldout.height;
                    labelRect.xMin += style.foldout.allocatedHorizontalSpace;
                }

                if (style.enableElementRemoveButton)
                {
                    removeButtonRect.Set(elementHeaderRect.xMax + style.removeElementButton.paddingLeft - style.removeElementButton.allocatedHorizontalSpace, elementHeaderRect.y, style.removeElementButton.width, style.removeElementButton.height);
                    labelRect.xMax -= style.removeElementButton.allocatedHorizontalSpace;
                    headerButtonRect.xMax -= style.removeElementButton.allocatedHorizontalSpace;
                }

                bodyRect.x = style.element.bodyPaddingLeft + listContentRect.x + RowNumberWidth;
                bodyRect.y = style.element.bodyPaddingTop + style.element.collapsedElementHeight + listContentRect.y;
                bodyRect.xMax = listContentRect.xMax - style.element.bodyPaddingRight;

                fieldRect.Set(bodyRect.x, bodyRect.y, bodyRect.width, 0);

                elementHeaderRectOffsetY = elementHeaderRect.y - listContentRect.y;
                draggingHandleRectOffsetY = draggingHandleRect.y - listContentRect.y;
                rowNumberRectOffsetY = rowNumberRect.y - listContentRect.y;
                foldoutRectOffsetY = foldoutRect.y - listContentRect.y;
                fieldRectOffsetY = bodyRect.y - listContentRect.y;

            }

            int viewStart = ViewStart();
            int viewEndExclusive = ViewEndExclusive();
            if (enablePagination)
            {
                //dealing with pages
                pagesCount = Mathf.CeilToInt((ViewCount() + 0f) / pageElementCount);

                if (pagesCount > 1) // fix to layout event bug
                {
                    SyncPageToSelectionIfNeeded();
                    currentPage = Mathf.Clamp(currentPage, 0, pagesCount - 1);
                }
                else
                {
                    SyncPageToSelectionIfNeeded();
                }

                pageBeginIndex = viewStart + currentPage * pageElementCount;
                currentBeginIndex = pageBeginIndex;
            }
            else
            {
                currentPage = 0;
                pageBeginIndex = viewStart;
                currentBeginIndex = pageBeginIndex;
                pagesCount = 1;
                SyncPageToSelectionIfNeeded();
            }

            if (viewEndExclusive > viewStart)
            {
                focusedElementIndex = FocusRealIndex(pageBeginIndex, viewEndExclusive);
                CalculateFocusedElementHeight();

                int pageEndExclusive = enablePagination
                    ? Mathf.Min(pageBeginIndex + pageElementCount, viewEndExclusive)
                    : viewEndExclusive;
                currenElementCount = Mathf.Max(0, pageEndExclusive - pageBeginIndex);
                currentBeginIndex = pageBeginIndex;
            }
            else
            {
                currenElementCount = 0;
                focusedElementHeight = CollapsedElementHeight;
            }


            filledElementsRect.Set(listContentRect.x, listContentRect.y, listContentRect.width, GetCurrentPageContentHeight());

            if (selectionScrollPending)
            {
                EnsureSelectedVisibleInScroll();
                selectionScrollPending = false;
            }
        }

        private float GetCurrentPageContentHeight()
        {
            if (currenElementCount <= 0)
            {
                return CollapsedElementHeight;
            }

            int selectedSlot = Filtering ? SlotOf(selectedIndex) : selectedIndex;
            if (useFoldout
                && selectedSlot >= currentBeginIndex
                && selectedSlot < currentBeginIndex + currenElementCount
                && focusedElementHeight > CollapsedElementHeight)
            {
                return (currenElementCount - 1) * CollapsedElementHeight + focusedElementHeight;
            }

            return currenElementCount * CollapsedElementHeight;
        }

        private void SyncPageToSelectionIfNeeded()
        {
            if (selectedIndex == lastPageSyncSelectedIndex)
            {
                return;
            }

            lastPageSyncSelectedIndex = selectedIndex;
            selectionScrollPending = true;
            if (selectedIndex < 0 || pageElementCount <= 0)
            {
                return;
            }

            int pageForSelection = PageForIndex(selectedIndex);
            if (pageForSelection != currentPage)
            {
                currentPage = pageForSelection;
                listScrollPosition = Vector2.zero;
            }
        }

        private void EnsureSelectedVisibleInScroll()
        {
            int selectedSlot = Filtering ? SlotOf(selectedIndex) : selectedIndex;
            if (selectedSlot < currentBeginIndex || selectedSlot >= currentBeginIndex + currenElementCount)
            {
                return;
            }

            float itemTop = (selectedSlot - currentBeginIndex) * CollapsedElementHeight;
            float itemBottom = itemTop + CollapsedElementHeight;
            float viewHeight = listContentRect.height;
            if (viewHeight <= 0f)
            {
                return;
            }

            if (itemTop < listScrollPosition.y)
            {
                listScrollPosition.y = itemTop;
            }
            else if (itemBottom > listScrollPosition.y + viewHeight)
            {
                listScrollPosition.y = itemBottom - viewHeight;
            }
        }

        private void GoToPage(int pageIndex)
        {
            int clamped = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, pagesCount - 1));
            if (clamped == currentPage)
            {
                return;
            }

            ClearMultiSelection();
            lastPageSyncSelectedIndex = selectedIndex;
            currentPage = clamped;
            listScrollPosition = Vector2.zero;
        }

        private float GetMouseYInListContent()
        {
            return currentEvent.mousePosition.y - listContentRect.position.y + listScrollPosition.y;
        }

        private void ApplyOffsets(float currentOffset)
        {
            elementHeaderRect.y = listContentRect.y + elementHeaderRectOffsetY + currentOffset;
            draggingHandleRect.y = listContentRect.y + draggingHandleRectOffsetY + currentOffset;
            rowNumberRect.y = listContentRect.y + rowNumberRectOffsetY + currentOffset;
            checkboxRect.y = elementHeaderRect.y + (elementHeaderRect.height - checkboxRect.height) * 0.5f;
            labelRect.y = elementHeaderRect.y;
            headerButtonRect.y = elementHeaderRect.y;
            foldoutRect.y = listContentRect.y + foldoutRectOffsetY + currentOffset;
            fieldRect.y = listContentRect.y + fieldRectOffsetY + currentOffset;
            removeButtonRect.y = listContentRect.y + elementHeaderRectOffsetY + currentOffset;
        }

        private void DrawHeader()
        {
            HandleDrawingBackgroundConfiguration(headerRect, style.header.backgroundConfiguration);
            EditorGUI.LabelField(headerContentRect, GetHeaderLabel(), style.header.labelStyle);
        }

        private void DrawList()
        {
            HandleDrawingBackgroundConfiguration(listRect, style.list.backgroundConfiguration);

            if (ViewCount() == 0)
            {
                HandleEmptyArray();
                return;
            }

            float contentHeight = GetCurrentPageContentHeight();
            bool needsScroll = contentHeight > listContentRect.height + 0.5f;
            float scrollbarWidth = needsScroll ? GUI.skin.verticalScrollbar.fixedWidth : 0f;
            float drawWidth = Mathf.Max(0f, listContentRect.width - scrollbarWidth);
            Rect contentRect = new Rect(0f, 0f, drawWidth, Mathf.Max(contentHeight, listContentRect.height));

            Rect savedListContentRect = listContentRect;
            Vector2 scrollForView = dragging ? dragLockedScrollPosition : listScrollPosition;
            listScrollPosition = GUI.BeginScrollView(listContentRect, scrollForView, contentRect, false, needsScroll);
            if (dragging)
            {
                // Keep scroll fixed while reordering so the list does not jump under the cursor.
                listScrollPosition = dragLockedScrollPosition;
            }

            listContentRect.x = 0f;
            listContentRect.y = 0f;
            listContentRect.width = drawWidth;
            listContentRect.height = contentHeight;
            filledElementsRect.Set(0f, 0f, drawWidth, contentHeight);

            tempX = 0f;
            tempY = 0f;

            if (dragging)
            {
                DrawListWhileDragging();
            }
            else
            {
                int endIndex = Mathf.Min(currentBeginIndex + currenElementCount, ViewEndExclusive());
                for (int slot = currentBeginIndex; slot < endIndex; slot++)
                {
                    int index = RealIndex(slot);
                    isSelected = IsSelected(index);

                    if (isSelected && useFoldout && index == selectedIndex)
                    {
                        elementRect.Set(tempX, tempY, listContentRect.width, focusedElementHeight);
                    }
                    else
                    {
                        elementRect.Set(tempX, tempY, listContentRect.width, CollapsedElementHeight);
                    }

                    tempY += elementRect.height;
                    visualRowIndex = index;
                    DrawElement(elementRect, isSelected, index);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                // Gutter stays put. The line is the edge of the draggable rows.
                EditorGUI.DrawRect(new Rect(elementHeaderRect.x, 0f, 1f, contentHeight), new Color(0f, 0f, 0f, 0.35f));
            }

            GUI.EndScrollView();
            if (dragging)
            {
                listScrollPosition = dragLockedScrollPosition;
            }

            listContentRect = savedListContentRect;
        }

        private void DrawListWhileDragging()
        {
            int arraySize = ArraySize();
            int endIndex = Mathf.Min(currentBeginIndex + currenElementCount, ViewEndExclusive());
            // Keep drop index inside both the visible window and the array.
            int dropIndex = Mathf.Clamp(currentDragIndex, currentBeginIndex, Mathf.Max(currentBeginIndex, endIndex - 1));

            if (draggingMulti && dragRemainingIndices != null)
            {
                DrawListWhileDraggingMulti(dropIndex, arraySize, endIndex);
                return;
            }

            // Single-item drag: map each visible slot without allocating a full remaining list.
            for (int slot = currentBeginIndex; slot < endIndex; slot++)
            {
                elementRect.Set(tempX, tempY, listContentRect.width, CollapsedElementHeight);

                int sourceIndex = GetDragSourceIndexForSlot(slot, dropIndex);
                visualRowIndex = slot;
                if (sourceIndex < 0)
                {
                    DrawDragInsertionGap(elementRect);
                    tempY += CollapsedElementHeight;
                    continue;
                }

                tempY += elementRect.height;
                DrawElement(elementRect, false, sourceIndex);
            }

            if (startDragIndex >= 0 && startDragIndex < arraySize)
            {
                elementRect.Set(tempX, draggedElementY, listContentRect.width, draggedElemenentHeight);
                visualRowIndex = -1;
                DrawElement(elementRect, true, startDragIndex);
            }
        }

        private void DrawListWhileDraggingMulti(int dropIndex, int arraySize, int endIndex)
        {
            List<int> remaining = dragRemainingIndices;
            for (int slot = currentBeginIndex; slot < endIndex; slot++)
            {
                elementRect.Set(tempX, tempY, listContentRect.width, CollapsedElementHeight);

                if (slot == dropIndex)
                {
                    visualRowIndex = slot;
                    DrawDragInsertionGap(elementRect);
                    tempY += CollapsedElementHeight;
                    continue;
                }

                int rank = slot < dropIndex ? slot : slot - 1;
                if (rank < 0 || rank >= remaining.Count)
                {
                    tempY += CollapsedElementHeight;
                    continue;
                }

                int sourceIndex = remaining[rank];
                tempY += elementRect.height;
                visualRowIndex = slot;
                DrawElement(elementRect, false, sourceIndex);
            }

            if (startDragIndex >= 0 && startDragIndex < arraySize)
            {
                elementRect.Set(tempX, draggedElementY, listContentRect.width, draggedElemenentHeight);
                visualRowIndex = -1;
                DrawElement(elementRect, true, startDragIndex);
                if (dragIndices != null && dragIndices.Count > 1 && Event.current.type == EventType.Repaint)
                {
                    Rect badgeRect = new Rect(elementRect.xMax - 36f, elementRect.y + 2f, 32f, 16f);
                    EditorGUI.DrawRect(badgeRect, new Color(0.15f, 0.35f, 0.75f, 0.9f));
                    GUI.Label(badgeRect, dragIndices.Count.ToString(), EditorStyles.centeredGreyMiniLabel);
                }
            }
        }

        private int GetDragSourceIndexForSlot(int slot, int dropIndex)
        {
            if (slot == dropIndex)
            {
                return -1;
            }

            if (dropIndex < startDragIndex)
            {
                // Moving up: open a gap at drop, shift items down toward the hole at start.
                if (slot < dropIndex)
                {
                    return slot;
                }

                if (slot <= startDragIndex)
                {
                    return slot - 1;
                }

                return slot;
            }

            if (dropIndex > startDragIndex)
            {
                // Moving down: open a gap at drop, shift items up toward the hole at start.
                if (slot < startDragIndex)
                {
                    return slot;
                }

                if (slot < dropIndex)
                {
                    return slot + 1;
                }

                return slot;
            }

            // dropIndex == startDragIndex: identity mapping, hole at start.
            return slot == startDragIndex ? -1 : slot;
        }

        private void DrawDragInsertionGap(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            ApplyOffsets(rect.y - listContentRect.y);
            DrawRowNumber();
            // Dim selected-style background marks the drop target without drawing another label.
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            DrawElementBackground(elementHeaderRect, true, style);
            GUI.color = previousColor;
        }

        private void HandleEmptyArray()
        {
            elementRect.Set(listContentRect.x, listContentRect.y, listContentRect.width, CollapsedElementHeight);
            GUI.Label(elementRect, style.EMPTY_LIST_LABEL);
        }

        #region Element



        private void DrawElement(Rect rect, bool isSelected, int index)
        {
            currentElementProperty = GetElement(index);
            ApplyOffsets(rect.y - listContentRect.y);
            DrawElementHeader(currentElementProperty, index, rect, style);

            //index < currentBeginIndex + currenElementCount is a fix that helps when user deletes element with header button
            int elementSlot = Filtering ? SlotOf(index) : index;
            if (isSelected && useFoldout && (IsExpanded(index)) && elementSlot >= currentBeginIndex && (elementSlot < currentBeginIndex + currenElementCount))
            {
                DrawElementBody(currentElementProperty, style);
            }

            // Reorder only from the == handle. Label / rest of row is click-to-select only.
            if ((!checkboxSelectionMode) && (!dragging) && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (displayedIndices == null && GetDragHandleHitRect().Contains(currentEvent.mousePosition))
                {
                    // Mouse position here is inside the scroll view. Store screen space so the
                    // drag check after EndScrollView uses the same coordinate system.
                    lastMouseDownPosition = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
                    lastMouseDownIndex = index;
                    lastMouseDownHeight = rect.height;
                    currentEvent.Use();
                }
                else if (rect.Contains(currentEvent.mousePosition))
                {
                    // Clicking the label must not keep a prior handle press and start a drag.
                    lastMouseDownIndex = -1;
                }
            }
        }

        private Rect GetDragHandleHitRect()
        {
            if (checkboxSelectionMode)
            {
                return Rect.zero;
            }

            // Hit only the == glyph column (not the label). Full row height, narrow width.
            const float hitPadX = 2f;
            Rect dragHitRect = draggingHandleRect;
            dragHitRect.xMin = Mathf.Max(elementHeaderRect.xMin, draggingHandleRect.xMin - hitPadX);
            dragHitRect.xMax = Mathf.Min(
                elementHeaderRect.xMin + style.dragHandle.allocatedHorizontalSpace,
                draggingHandleRect.xMax + hitPadX);
            dragHitRect.yMin = elementHeaderRect.yMin;
            dragHitRect.yMax = elementHeaderRect.yMax;
            return dragHitRect;
        }

        private void DrawRowNumber()
        {
            if (visualRowIndex < 0 || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (rowNumberStyle == null)
            {
                rowNumberStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 10,
                    normal = { textColor = new Color(0.55f, 0.55f, 0.55f, 1f) }
                };
            }

            GUI.Label(rowNumberRect, (visualRowIndex + 1).ToString(), rowNumberStyle);
        }

        private void DrawElementHeader(SerializedProperty currentElementProperty, int index, Rect elementRect, CustomListStyle style)
        {
            const float checkboxSize = 14f;
            checkboxRect.Set(
                elementHeaderRect.x + 3f,
                elementHeaderRect.y + (elementHeaderRect.height - checkboxSize) * 0.5f,
                checkboxSize,
                checkboxSize);

            if (!checkboxSelectionMode)
            {
                Rect dragHitRect = GetDragHandleHitRect();
                EditorGUIUtility.AddCursorRect(dragHitRect, MouseCursor.Pan);
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawElementBackground(elementHeaderRect, isSelected, style);
                DrawRowNumber();
                if (!checkboxSelectionMode)
                {
                    style.dragHandle.guiStyle.Draw(draggingHandleRect, false, false, false, false);
                }

                if (useFoldout)
                {
                    style.foldout.guiStyle.Draw(foldoutRect, false, currentElementProperty.isExpanded, currentElementProperty.isExpanded, currentElementProperty.isExpanded);
                }
            }

            if (checkboxSelectionMode)
            {
                EditorGUI.BeginChangeCheck();
                bool isChecked = IsSelected(index);
                bool newChecked = EditorGUI.Toggle(checkboxRect, isChecked);
                if (EditorGUI.EndChangeCheck() && newChecked != isChecked)
                {
                    ToggleMultiSelection(index);
                    if (currentEvent != null)
                    {
                        currentEvent.Use();
                    }
                }
            }

            if (useLabelProperty)
            {
                GUI.Label(labelRect, currentElementProperty.FindPropertyRelative(labelPropertyName).stringValue);
            }
            else
            {
                GUI.Label(labelRect, getLabelCallback(currentElementProperty, index));
            }

            if (style.enableElementRemoveButton)
            {
                GUI.Label(removeButtonRect, style.removeElementButton.content, style.removeElementButton.guiStyle);

                if ((!dragging) && (currentEvent.type == EventType.MouseUp) && removeButtonRect.Contains(currentEvent.mousePosition))
                {
                    SetSingleSelection(index, invokeCallback: false);
                    RemoveElement();
                    currentEvent.Use();
                }
            }

            

            headerButtonRect.Set(elementHeaderRect.x, elementHeaderRect.y, elementHeaderRect.width, elementHeaderRect.height);
            headerButtonRect.xMax = labelRect.xMax;
            // Keep selection / foldout clicks off the drag handle / checkbox.
            if (checkboxSelectionMode)
            {
                headerButtonRect.xMin = checkboxRect.xMax + 2f;
            }
            else
            {
                headerButtonRect.xMin = GetDragHandleHitRect().xMax;
            }

            
            if ((!dragging) && (currentEvent.type == EventType.MouseUp))
            {
                if (checkboxSelectionMode
                    && currentEvent.button == 1
                    && elementHeaderRect.Contains(currentEvent.mousePosition))
                {
                    HandleRowSelectionClick(index, 1);
                    currentEvent.Use();
                }
                else if (headerButtonRect.Contains(currentEvent.mousePosition)
                    || rowNumberRect.Contains(currentEvent.mousePosition))
                {
                    if (checkboxSelectionMode && currentEvent.button == 0)
                    {
                        ActivateWithoutClearingOthers(index);
                        if (useFoldout && selectedIndex == index)
                        {
                            currentElementProperty.isExpanded = !currentElementProperty.isExpanded;
                        }
                    }
                    else
                    {
                        HandleRowSelectionClick(index, currentEvent.button);

                        if (currentEvent.button == 0 && useFoldout && selectedIndex == index)
                        {
                            currentElementProperty.isExpanded = !currentElementProperty.isExpanded;
                        }
                    }

                    currentEvent.Use();
                }
            }

        }

        private void DrawElementBody(SerializedProperty currentElementProperty, CustomListStyle style)
        {
            HandleDrawingBackgroundConfiguration(bodyRect, style.element.elementBodyBackgroundConfiguration);

            for (int i = 0; i < fields.Count; i++)
            {
                fieldRect.height = fieldHeights[i];
                fields[i].Draw(currentElementProperty, fieldRect, style);
                fieldRect.y += fieldRect.height;
            }
        }

        private void DrawElementBackground(Rect rect, bool isSelected, CustomListStyle style)
        {
            foreach (CustomListStyle.ElementColorRect colorRect in style.element.elementHeaderBackgroundConfiguration.rects)
            {
                if ((colorRect.drawType == CustomListStyle.DrawType.DrawWhenSelected) && (!isSelected))
                {
                    continue;
                }

                if ((colorRect.drawType == CustomListStyle.DrawType.DrawWhenUnselected) && (isSelected))
                {
                    continue;
                }

                backgroundRect.x = colorRect.paddingLeft + rect.x;
                backgroundRect.y = colorRect.paddingTop + rect.y;
                backgroundRect.xMax = rect.xMax - colorRect.paddingRight;
                backgroundRect.yMax = rect.yMax - colorRect.paddingBottom;

                if (colorRect.rectType == CustomListStyle.RectType.ColorRect)
                {
                    EditorGUI.DrawRect(backgroundRect, colorRect.color);
                }
                else if (colorRect.rectType == CustomListStyle.RectType.Border)
                {
                    GUI.DrawTexture(backgroundRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, colorRect.color, colorRect.borderWidth, colorRect.borderRadius);
                }
                else if (colorRect.rectType == CustomListStyle.RectType.RoundRect)
                {
                    GUI.DrawTexture(backgroundRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, colorRect.color, colorRect.borderWidth * 100, colorRect.borderRadius);
                }
            }

        }

        private void CalculateFocusedElementHeight()
        {
            focusedElementProperty = GetElement(focusedElementIndex);

            if(useFoldout && IsExpanded(focusedElementIndex))
            {
                bodyHeight = 0;

                for (int i = 0; i < fields.Count; i++)
                {
                    fieldHeights[i] = fields[i].GetHeight(focusedElementProperty, style);
                    bodyHeight += fieldHeights[i];
                }

                bodyHeight += style.element.bodyPaddingTop + style.element.bodyPaddingBottom;
                focusedElementHeight = style.element.collapsedElementHeight + bodyHeight;
            }
            else
            {
                focusedElementHeight = CollapsedElementHeight;
            }
        }
        #endregion

        #region style functions
        private void HandleDrawingBackgroundConfiguration(Rect rect, CustomListStyle.BackgroundConfiguration configuration)
        {
            if (currentEvent.type == EventType.Repaint && (configuration.rects != null))
            {
                foreach (CustomListStyle.ColorRect colorRect in configuration.rects)
                {
                    backgroundRect.x = colorRect.paddingLeft + rect.x;
                    backgroundRect.y = colorRect.paddingTop + rect.y;
                    backgroundRect.xMax = rect.xMax - colorRect.paddingRight;
                    backgroundRect.yMax = rect.yMax - colorRect.paddingBottom;

                    if(colorRect.rectType == CustomListStyle.RectType.ColorRect)
                    {
                        EditorGUI.DrawRect(backgroundRect, colorRect.color);
                    }
                    else if (colorRect.rectType == CustomListStyle.RectType.Border)
                    {
                        GUI.DrawTexture(backgroundRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, colorRect.color, colorRect.borderWidth, colorRect.borderRadius);
                    }
                    else if (colorRect.rectType == CustomListStyle.RectType.RoundRect)
                    {
                        GUI.DrawTexture(backgroundRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, colorRect.color, colorRect.borderWidth*100, colorRect.borderRadius);
                    }



                }
            }
        }
        #endregion



        #region Drag

        private const float MIN_DRAG_START_DISTANCE = 4f;

        private void HandleDraggingDetection()
        {
            if (ArraySize() <= 0)
            {
                CancelDragging();
                return;
            }

            // Always resolve an in-progress drag, even if board painting set IgnoreDragEvents
            // or another control already called Event.Use() (rawType still reports MouseUp).
            EventType eventType = currentEvent.rawType;

            if (dragging)
            {
                if (eventType == EventType.MouseDrag || eventType == EventType.MouseMove)
                {
                    UpdateDrag();
                }
                else if (eventType == EventType.MouseUp || eventType == EventType.Ignore)
                {
                    DraggingFinished();
                    lastMouseDownIndex = -1;
                }
                else if (eventType == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
                {
                    CancelDragging();
                    currentEvent.Use();
                }

                return;
            }

            if (ignoreDragEvents || currentEvent.type == EventType.Used)
            {
                if (eventType == EventType.MouseUp)
                {
                    lastMouseDownIndex = -1;
                }

                return;
            }

            // Start reorder after the pointer moves a few pixels.
            // lastMouseDownPosition is screen space; this check runs outside the scroll view.
            if (currentEvent.type == EventType.MouseDrag && lastMouseDownIndex >= 0)
            {
                Vector2 now = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
                float dragDistance = (lastMouseDownPosition - now).magnitude;
                if (dragDistance >= MIN_DRAG_START_DISTANCE)
                {
                    DraggingStarted();
                }
            }
            else if (currentEvent.type == EventType.MouseUp || eventType == EventType.MouseUp)
            {
                lastMouseDownIndex = -1;
            }
        }

        private void DraggingStarted()
        {
            if (checkboxSelectionMode || displayedIndices != null)
            {
                lastMouseDownIndex = -1;
                return;
            }

            dragging = true;
            dragLockedScrollPosition = listScrollPosition;
            startDragIndex = Mathf.Clamp(lastMouseDownIndex, 0, ArraySize() - 1);

            // Dragging an unselected row replaces selection with that row (single drag).
            if (!selectedIndices.Contains(startDragIndex))
            {
                SetSingleSelection(startDragIndex, invokeCallback: false);
            }

            dragIndices = GetSortedSelectedIndices();
            draggingMulti = dragIndices.Count > 1;
            currentDragIndex = startDragIndex;
            draggedElemenentHeight = Mathf.Max(lastMouseDownHeight, CollapsedElementHeight);
            RebuildDragRemainingIndices();

            // Anchor the floating row to the pressed item, not a stale offset from a previous drag.
            float itemContentY = (startDragIndex - currentBeginIndex) * CollapsedElementHeight;
            dragOffset = GetMouseYInListContent() - itemContentY;
            float contentHeight = GetCurrentPageContentHeight();
            draggedElementY = Mathf.Clamp(
                itemContentY,
                0f,
                Mathf.Max(0f, contentHeight - draggedElemenentHeight));

            isDraggedElementExpanded = IsExpanded(startDragIndex);
            currentEvent.Use();

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            }
        }

        private void RebuildDragRemainingIndices()
        {
            if (!draggingMulti || dragIndices == null)
            {
                dragRemainingIndices = null;
                return;
            }

            HashSet<int> moving = new HashSet<int>(dragIndices);
            int arraySize = ArraySize();
            if (dragRemainingIndices == null)
            {
                dragRemainingIndices = new List<int>(arraySize);
            }
            else
            {
                dragRemainingIndices.Clear();
            }

            for (int i = 0; i < arraySize; i++)
            {
                if (!moving.Contains(i))
                {
                    dragRemainingIndices.Add(i);
                }
            }
        }

        private void UpdateDrag()
        {
            int arraySize = ArraySize();
            int visibleCount = Mathf.Min(currenElementCount, Mathf.Max(0, arraySize - currentBeginIndex));
            int maxIndex = currentBeginIndex + Mathf.Max(0, visibleCount - 1);

            mouseYinList = GetMouseYInListContent();
            int hoverIndex = currentBeginIndex + Mathf.FloorToInt(mouseYinList / CollapsedElementHeight);
            currentDragIndex = Mathf.Clamp(hoverIndex, currentBeginIndex, maxIndex);
            float contentHeight = GetCurrentPageContentHeight();
            draggedElementY = Mathf.Clamp(
                mouseYinList - dragOffset,
                0f,
                Mathf.Max(0f, contentHeight - draggedElemenentHeight));

            if (currentDragIndex == startDragIndex)
            {
                dragIndexAdjustment = 0;
            }
            else if (currentDragIndex < startDragIndex)
            {
                dragIndexAdjustment = 1;
            }
            else
            {
                dragIndexAdjustment = -1;
            }

            previousDraggedElementY = draggedElementY;

            if (currentEvent.type != EventType.Used)
            {
                currentEvent.Use();
            }

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            }
        }

        private void RequestRepaint()
        {
            if (usingListInterface)
            {
                return;
            }

            EditorUtility.SetDirty(serializedObject.targetObject);
        }

        private void CancelDragging()
        {
            if (!dragging)
            {
                lastMouseDownIndex = -1;
                return;
            }

            dragging = false;
            draggingMulti = false;
            dragIndices = null;
            dragRemainingIndices = null;
            lastMouseDownIndex = -1;
            dragIndexAdjustment = 0;

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            }
        }

        private void DraggingFinished()
        {
            if (!dragging)
            {
                return;
            }

            int from = startDragIndex;
            int to = currentDragIndex;
            bool wasMulti = draggingMulti;
            List<int> moved = dragIndices != null ? new List<int>(dragIndices) : null;
            dragging = false;
            draggingMulti = false;
            dragIndices = null;
            dragRemainingIndices = null;
            dragIndexAdjustment = 0;
            listScrollPosition = dragLockedScrollPosition;

            if (wasMulti && moved != null && moved.Count > 1)
            {
                MoveSelectedElementsTo(to, moved);
            }
            else if (from != to && from >= 0 && to >= 0 && from < ArraySize() && to < ArraySize())
            {
                MoveElement(from, to);
                SetSingleSelection(to, invokeCallback: false);
                // Keep page/scroll where the user dropped; do not auto-scroll to selection.
                lastPageSyncSelectedIndex = selectedIndex;
                selectionScrollPending = false;
                // Same level asset — do not reopen the right-hand inspector.
            }
            else if (from >= 0 && from < ArraySize())
            {
                if (!IsSelected(from))
                {
                    SetSingleSelection(from, invokeCallback: false);
                }
                else
                {
                    selectedIndex = from;
                }

                lastPageSyncSelectedIndex = selectedIndex;
                selectionScrollPending = false;
                selectionChangedCallback?.Invoke();
            }

            if (useFoldout && isDraggedElementExpanded)
            {
                for (int i = currentBeginIndex; i < currentBeginIndex + currenElementCount; i++)
                {
                    if (i == selectedIndex)
                    {
                        SetIsExpanded(i, isDraggedElementExpanded);
                    }
                    else
                    {
                        SetIsExpanded(i, false);
                    }
                }
            }

            if (currentEvent.type != EventType.Used)
            {
                currentEvent.Use();
            }
        }

        /// <summary>
        /// Moves selected indices as one ordered block so the block starts at dropIndex (adjusted for removals).
        /// </summary>
        private void MoveSelectedElementsTo(int dropIndex, List<int> sortedSelected)
        {
            int size = ArraySize();
            if (sortedSelected == null || sortedSelected.Count == 0 || size <= 0)
            {
                return;
            }

            dropIndex = Mathf.Clamp(dropIndex, 0, size - 1);

            // No-op when dropping inside an already-contiguous selection block.
            bool contiguous = true;
            for (int i = 1; i < sortedSelected.Count; i++)
            {
                if (sortedSelected[i] != sortedSelected[i - 1] + 1)
                {
                    contiguous = false;
                    break;
                }
            }

            if (contiguous && dropIndex >= sortedSelected[0] && dropIndex <= sortedSelected[sortedSelected.Count - 1])
            {
                selectedIndex = Mathf.Clamp(selectedIndex, sortedSelected[0], sortedSelected[sortedSelected.Count - 1]);
                selectionChangedCallback?.Invoke();
                return;
            }

            int removedBeforeDrop = 0;
            for (int i = 0; i < sortedSelected.Count; i++)
            {
                if (sortedSelected[i] < dropIndex)
                {
                    removedBeforeDrop++;
                }
            }

            int insertIndex = Mathf.Clamp(dropIndex - removedBeforeDrop, 0, size - sortedSelected.Count);

            if (usingListInterface)
            {
                UndoCallback();
                var items = new List<object>(sortedSelected.Count);
                for (int i = 0; i < sortedSelected.Count; i++)
                {
                    items.Add(elements[sortedSelected[i]]);
                }

                for (int i = sortedSelected.Count - 1; i >= 0; i--)
                {
                    elements.RemoveAt(sortedSelected[i]);
                }

                for (int i = 0; i < items.Count; i++)
                {
                    elements.Insert(insertIndex + i, items[i]);
                }
            }
            else if (usingPropertyList)
            {
                UndoCallback();
                var items = new List<SerializedProperty>(sortedSelected.Count);
                for (int i = 0; i < sortedSelected.Count; i++)
                {
                    items.Add(elementsList[sortedSelected[i]]);
                }

                for (int i = sortedSelected.Count - 1; i >= 0; i--)
                {
                    elementsList.RemoveAt(sortedSelected[i]);
                }

                for (int i = 0; i < items.Count; i++)
                {
                    elementsList.Insert(insertIndex + i, items[i]);
                }
            }
            else
            {
                // Only move array slots (pointers). Do not load LevelModel via objectReferenceValue.
                // Reorder is intentionally not undoable — skip CompleteObjectUndo.
                for (int i = 0; i < sortedSelected.Count; i++)
                {
                    int src = sortedSelected[i] - i;
                    elementsProperty.MoveArrayElement(src, size - 1);
                }

                int blockStart = size - sortedSelected.Count;
                for (int i = 0; i < sortedSelected.Count; i++)
                {
                    elementsProperty.MoveArrayElement(blockStart + i, insertIndex + i);
                }

                ApplyElementsPropertyChangesWithoutUndo();
            }

            selectedIndices.Clear();
            for (int i = 0; i < sortedSelected.Count; i++)
            {
                selectedIndices.Add(insertIndex + i);
            }

            selectedIndex = insertIndex;
            selectionAnchor = insertIndex;
            lastPageSyncSelectedIndex = selectedIndex;
            selectionScrollPending = false;
            LastReorderFrom = -1;
            LastReorderTo = insertIndex;
            listReorderedCallback?.Invoke();
            ListChangedCallback();
            // Multi-drag keeps the same assets selected — skip reopening the inspector.
        }

        #endregion

        private void DrawPagination()
        {
            HandleDrawingBackgroundConfiguration(footerPaginationRect, style.pagination.backgroundConfiguration);

            using (new EditorGUI.DisabledScope(currentPage <= 1))
            {
                if (GUI.Button(firstPageButtonRect, style.pagination.firstPageContent, style.pagination.buttonStyle))
                {
                    GoToPage(0);
                }
            }

            using (new EditorGUI.DisabledScope(currentPage == 0))
            {
                if (GUI.Button(previousPageButtonRect, style.pagination.previousPageContent, style.pagination.buttonStyle))
                {
                    GoToPage(currentPage - 1);
                }
            }

            // Shared metrics so the editable page and "/total" sit on the same center line.
            GUIStyle pageFieldStyle = new GUIStyle(EditorStyles.miniTextField)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                clipping = TextClipping.Clip,
                contentOffset = Vector2.zero,
                padding = new RectOffset(2, 2, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            // Plain text only — no text-field background on the total page count.
            GUIStyle totalLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = pageFieldStyle.fontSize,
                clipping = TextClipping.Clip,
                contentOffset = Vector2.zero,
                padding = new RectOffset(3, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            EditorGUI.BeginChangeCheck();
            string pageText = EditorGUI.DelayedTextField(paginationPageFieldRect, (currentPage + 1).ToString(), pageFieldStyle);
            if (EditorGUI.EndChangeCheck() && int.TryParse(pageText, out int pageInput))
            {
                GoToPage(pageInput - 1);
            }

            GUI.Label(paginationTotalLabelRect, "/" + pagesCount, totalLabelStyle);


            using (new EditorGUI.DisabledScope(currentPage == pagesCount - 1))
            {
                if (GUI.Button(nextPageButtonRect, style.pagination.nextPageContent, style.pagination.buttonStyle))
                {
                    GoToPage(currentPage + 1);
                }
            }

            using (new EditorGUI.DisabledScope(currentPage >= pagesCount - 2))
            {
                if (GUI.Button(lastPageButtonRect, style.pagination.lastPageContent, style.pagination.buttonStyle))
                {
                    GoToPage(pagesCount - 1);
                }
            }
        }

        private void DrawFooterButtons()
        {
            rightEdge = footerButtonsRect.xMax - style.footerButtons.marginRight - style.footerButtons.spaceBetweenButtons;
            leftEdge = rightEdge - style.footerButtons.paddingLeft - style.footerButtons.paddingRight;
            leftEdge -= style.footerButtons.buttonsWidth; // space for one button

            if (style.enableFooterRemoveButton || style.enableFooterAddButton)
            {
                leftEdge -= style.footerButtons.buttonsWidth; //space for other button
            }

            buttonsRect.Set(leftEdge, footerButtonsRect.y, rightEdge - leftEdge, footerButtonsRect.height);

            footerButtonRect.Set(leftEdge + style.footerButtons.paddingLeft, buttonsRect.y, style.footerButtons.buttonsWidth, style.footerButtons.buttonsHeight);

            HandleDrawingBackgroundConfiguration(buttonsRect, style.footerButtons.backgroundConfiguration);

            if (style.enableFooterAddButton)
            {
                if(addElementWithDropdownCallback != null)
                {
                    if (GUI.Button(footerButtonRect, style.footerButtons.addButtonWithDropdown, style.footerButtons.buttonStyle))
                    {
                        addElementWithDropdownCallback.Invoke();
                    }
                }
                else
                {
                    if (GUI.Button(footerButtonRect, style.footerButtons.addButton, style.footerButtons.buttonStyle))
                    {
                        AddElement();
                    }
                }
                //add button

                footerButtonRect.x += style.footerButtons.buttonsWidth + style.footerButtons.spaceBetweenButtons;
            }

            if (style.enableFooterRemoveButton)
            {
                //remove button
                using (new EditorGUI.DisabledScope(SelectedCount <= 0))
                {
                    if (GUI.Button(footerButtonRect, style.footerButtons.removeButton, style.footerButtons.buttonStyle))
                    {
                        RemoveElement();
                    }
                }
            }
        }

        private void CollapseElements()
        {
            if (!useFoldout)
            {
                return;
            }

            for (int i = 0; i < ArraySize(); i++)
            {
                if (i != selectedIndex)
                {
                    SetIsExpanded(i, false);
                }


            }
        }

        #region handle callbacks
        private string GetHeaderLabel()
        {
            return getHeaderLabelCallback?.Invoke();
        }

        private void OnSelectionChanged(int index, bool enableCollapse = true)
        {
            selectedIndex = index;
            if (index >= 0)
            {
                if (!selectedIndices.Contains(index))
                {
                    selectedIndices.Clear();
                    selectedIndices.Add(index);
                }

                if (selectionAnchor < 0)
                {
                    selectionAnchor = index;
                }
            }
            else
            {
                selectedIndices.Clear();
                selectionAnchor = -1;
            }

            if (enableCollapse)
            {
                CollapseElements();
            }
            
            selectionChangedCallback?.Invoke();
        }

        public void ListChangedCallback()
        {
            listChangedCallback?.Invoke();
        }

        private void AddElement()
        {
            addElementCallback?.Invoke();
            ListChangedCallback();
        }

        private protected void RemoveElement()
        {
            currenElementCount--;
            removeElementCallback?.Invoke();
            ListChangedCallback();
        }

        private void DisplayContextMenu()
        {
            displayContextMenuCallback?.Invoke();
        }

        private void UndoCallback()
        {
            listUndoCallback?.Invoke();
        }

        #endregion
    }
}
#endif