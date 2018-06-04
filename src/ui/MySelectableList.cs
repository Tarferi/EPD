using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace StarcraftEPDTriggers.src.ui {

    public class MySelectableListItem {

        private object _obj;
        protected bool _isSelected;
        private MySelectableList _lst;
        
        private FrameworkElement _element;

        public object CustomData { get { return _obj; } }

        public FrameworkElement Element { get { return _wrapper; } }

        private Grid _wrapper;

        public MySelectableListItem(object obj, FrameworkElement element) {
            _isSelected = false;
            _obj = obj;
            _element = element;

            _wrapper = new Grid();
            _wrapper.Focusable = true;
            _wrapper.IsHitTestVisible = true;
            if(_element.Parent != null) {
                var parent = _element.Parent;
                if(parent is Grid) {
                    Grid g = (Grid)parent;
                    g.Children.Remove(_element);
                }
            }
            _wrapper.Children.Add(_element);
            _wrapper.HorizontalAlignment = HorizontalAlignment.Stretch;

            _wrapper.PreviewMouseDown += (object sender, MouseButtonEventArgs args) => {

                if(args.ChangedButton == MouseButton.Left) {
                    if (args.ClickCount > 1) {
                        _lst.DoubleClickMe(this);
                    } else {
                        _lst.SelectMe(this);
                    }
                }
            };

            _wrapper.PreviewKeyDown += (object sender, KeyEventArgs args) => {
                if (_isSelected) {
                    if (args.Key == Key.Down) {
                        _lst.SelectNextItem(this);
                    } else if (args.Key == Key.Up) {
                        _lst.SelectPreviousItem(this);
                    } else if (args.Key == Key.Home) {
                        _lst.SelectFirstItem();
                    } else if (args.Key == Key.End) {
                        _lst.SelectLastItem();
                    }
                }
            };
        }

        public void __unselect() {  // No fkin touch this
            _isSelected = false;
        }

        public void __select() { // No fkin touch this
            _isSelected = true;
            _wrapper.Focus();
        }

        public void __setList(MySelectableList lst) { // No fkin touch this
            _lst = lst;
            if (_lst != null) {
                _element.Margin = _lst.Spacing;
            }
        }
    }

    public class MySelectableList {

        private Panel _panel;
        private Thickness _spacing;

        public Thickness Spacing { get { return _spacing; } }

        public event SelectionChanged SelectionChange = delegate { };

        public event DoubleClicked DoubleClick = delegate { };

        public delegate void DoubleClicked(MySelectableListItem item);

        public delegate void SelectionChanged(MySelectableListItem lastSelected, MySelectableListItem newSelected);

        private MySelectableListItem _currentlySelected;
        private int _selectedIndex;

        public MySelectableListItem CurrentlySelected { get { return _currentlySelected; } set { handleSelectionDispatch(_currentlySelected, value); } }

        private Dictionary<UIElement, MySelectableListItem> backMapping = new Dictionary<UIElement, MySelectableListItem>();

        public bool isEnabled { get { return _panel.IsEnabled; } set { _panel.IsEnabled = value; } }

        public Dispatcher Dispatcher { get { return _panel.Dispatcher; } }

        private void handleSelectionDispatch(MySelectableListItem lastSelected, MySelectableListItem newSelected) {
            if(lastSelected != newSelected) {
                _currentlySelected = newSelected;
                SelectionChange(lastSelected, newSelected);
            }
        }

        public void DoubleClickMe(MySelectableListItem item) {
            SelectMe(item);
            DoubleClick(item);
        }

        public void SelectMe(MySelectableListItem item) {
            int indexss = _panel.Children.IndexOf(item.Element);
            if (item != CurrentlySelected) {
                int index = _panel.Children.IndexOf(item.Element);
                if(index >=0) {
                    SelectedIndex = index;
                }
            }
        }

        public int SelectedIndex { get { return _selectedIndex; } set { select(value); } }

        public bool isFirstItemSelected() {
            return _selectedIndex == 0;
        }

        public bool isLastItemSelected() {
            return _selectedIndex == _panel.Children.Count - 1;
        }

        public void SelectPreviousItem(MySelectableListItem item) {
            int index = _panel.Children.IndexOf(item.Element);
            int count = _panel.Children.Count;
            if (index == 0) {
                return;
            }
            if (index < 0 || index >= count) {
                SelectedIndex = -1;
            } else {
                SelectedIndex = index - 1;
            }
        }

        public void SelectNextItem(MySelectableListItem item) {
            int index = _panel.Children.IndexOf(item.Element);
            int count = _panel.Children.Count;
            if(index == count -1) { // Keep selection
                return;
            }
            if (index < 0 || index >= count) {
                SelectedIndex = -1;
            } else {
                SelectedIndex = index + 1;
            }
        }

        public void MoveItemUp(MySelectableListItem item) {
            int index = _panel.Children.IndexOf(item.Element);
            if (index > 0) { // Can move up
                UIElement there = _panel.Children[index];
                _panel.Children.RemoveAt(index);
                _panel.Children.Insert(index-1, there);
                if (item == CurrentlySelected) {
                    _selectedIndex--;
                }
                SelectionChange(CurrentlySelected, CurrentlySelected);
            }
        }

        public void MoveItemDown(MySelectableListItem item) {
            int index = _panel.Children.IndexOf(item.Element);
            if (index >= 0 && index < _panel.Children.Count-1) {// Can move down
                UIElement there = _panel.Children[index];
                _panel.Children.RemoveAt(index);
                _panel.Children.Insert(index+1, there);
                if (item == CurrentlySelected) {
                    _selectedIndex++;
                }
                SelectionChange(CurrentlySelected, CurrentlySelected);
            }
        }

        public void MoveItemBeforeItem(MySelectableListItem what, MySelectableListItem beforeWhat) {
            _panel.Children.Remove(what.Element);
            _panel.Children.Insert(_panel.Children.IndexOf(beforeWhat.Element), what.Element);
            if (_selectedIndex != -1) {
                _selectedIndex = _panel.Children.IndexOf(CurrentlySelected.Element);
            }
            SelectionChange(CurrentlySelected, CurrentlySelected);
        }

        public void Unselect() {
            SelectedIndex = -1;
        }

        public void SelectFirstItem() {
            int count = _panel.Children.Count;
            if (count == 0) {
                SelectedIndex = -1;
            } else {
                SelectedIndex = 0;
            }
        }

        public void SelectLastItem() {
            int count = _panel.Children.Count;
            if (count == 0) {
                SelectedIndex = -1;
            } else {
                SelectedIndex = count - 1;
            }
        }

        public MySelectableList(Panel panel) :this(panel,new Thickness(0,0,0,0)) { }

        public MySelectableList(Panel panel, Thickness spacing) {
            _panel = panel;
            SelectedIndex = -1;
            _spacing = spacing;
        }

        public void Add(MySelectableListItem element) {
            _panel.Children.Add(element.Element);
            element.__setList(this);
            backMapping.Add(element.Element, element);
        }

        public void Remove(MySelectableListItem element) {
            _panel.Children.Remove(element.Element);
            element.__setList(null);
            backMapping.Remove(element.Element);
            if (CurrentlySelected == element) {
                CurrentlySelected = null;
                SelectedIndex = -1;
            }
        }

        public List<MySelectableListItem> GetAllItems() {
            List<MySelectableListItem> lst = new List<MySelectableListItem>();
            foreach(UIElement item in _panel.Children) {
                if(backMapping.ContainsKey(item)) {
                    lst.Add(backMapping[item]);
                } else {
                    throw new NotImplementedException();
                }
            }
            return lst;
        }

        public void Clear() {
            _panel.Children.Clear();
            backMapping.Clear();
            CurrentlySelected = null;
            SelectedIndex = -1;
        }

        private void select(int value) {
            if (value == -1) { // Unselect
                _selectedIndex = value;
            } else if (value < _panel.Children.Count) {
                _selectedIndex = value;
            } else { // Selecting index beyond the scope
                _selectedIndex = -1;
            }

            if (CurrentlySelected != null) {
                CurrentlySelected.__unselect();
            }

            if(_selectedIndex == -1) {
                CurrentlySelected = null;
                return;
            }

            UIElement sel = _panel.Children[value];
            MySelectableListItem newlySelectedItem = backMapping[sel];
            CurrentlySelected = newlySelectedItem;
            CurrentlySelected.__select();
        }

        /*
        public void AddAfter(MySelectableListItem item, Func<MySelectableListItem, bool> itemIdentifier) {

            int index = 0;
            foreach (UIElement el in _panel.Children) {
                if (backMapping.ContainsKey(el)) {
                    if (itemIdentifier((backMapping[el]))) {


                        return;
                    }
                } else {
                    throw new NotImplementedException();
                }
                index++;
            }
            throw new NotImplementedException();
        }
        */
    }
}
