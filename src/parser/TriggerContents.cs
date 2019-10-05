using StarcraftEPDTriggers.src.data;
using StarcraftEPDTriggers.src.ui;
using StarcraftEPDTriggers.src.wnd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using StarcraftEPDTriggers;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace StarcraftEPDTriggers.src {

    public class BitmapImageX : StackPanel {

        private InnerBitmapImageX _inner;
        private Label _lbl;

        private object _value;

        private bool _hover = false;
        private bool _sel = false;

        public bool IsSelected { set { _sel = value; _hover = false; selChanged(value); } get { return _sel; } }
        public bool Hover { set { _hover = value; InvalidateVisual(); } get { return _hover; } }


        public static readonly Brush hoverBrush = new SolidColorBrush(Color.FromArgb(80, 100, 100, 255));
        public static readonly Brush selBrush = new SolidColorBrush(Color.FromArgb(200, 100, 255, 100));
        public static readonly Brush defBrush = new SolidColorBrush(Colors.Transparent);
        public static readonly Brush blackBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        public static readonly Brush hoverBrushRaw = new SolidColorBrush(Color.FromArgb(100, 206, 206, 255));
        public static readonly Brush selBrushRaw = new SolidColorBrush(Color.FromArgb(100, 133, 255, 133));

        private void selChanged(bool newValue) {
            if (newValue) {
                Background = selBrush;
            } else {
                Background = defBrush;
            }
            _inner.InvalidateVisual();
        }

        public object GetValue() {
            return _value;
        }

        public override string ToString() {
            return _value.ToString();
        }

        public void SetValue(object value) {
            _value = value;
        }

        private BitmapImageX() {
        }

        public BitmapImageX(string path, object value) {
            _value = value;
            _lbl = new Label();
            _lbl.Content = value.ToString();
            _inner = new InnerBitmapImageX(path, this);
            setup();
        }

        private void setup() {
            Orientation = Orientation.Vertical;
            if (_lbl.Content.ToString().Length > 0) {
                _lbl.HorizontalAlignment = HorizontalAlignment.Center;
                Children.Add(_lbl);
            }
            Children.Add(_inner);
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            Brush defbg = Background;
            MouseEnter += delegate {
                if (!IsSelected) {
                    Hover = true;
                    Background = hoverBrush;
                    _inner.InvalidateVisual();
                }
            };
            MouseLeave += delegate {
                if (!IsSelected) {
                    Hover = false;
                    Background = defBrush;
                    _inner.InvalidateVisual();
                }
            };
            selChanged(IsSelected);
        }

        public BitmapImageX getCloned() {
            BitmapImageX x = new BitmapImageX();
            x._value = _value;
            x._lbl = new Label();
            x._lbl.Content = x._value.ToString();
            x.Width = Width;
            x.Height = Height;
            x._inner = _inner.getClonned(x);
            x.setup();
            return x;
        }

        public BitmapImage Image { get { return _inner.Image; } }



    }

    public class InnerBitmapImageX : Canvas {

        private static int ids = 0;
        private BitmapImage _img;
        private int ID = ids++;

        private BitmapImageX _source;

        private static readonly Pen pen = new Pen();

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) {
            Rect rect = new Rect(0, 0, ActualWidth, ActualHeight);
            drawingContext.DrawImage(_img, rect);
            if (_source.IsSelected) {
                drawingContext.DrawRectangle(BitmapImageX.selBrushRaw, pen, rect);
            } else if (_source.Hover) {
                drawingContext.DrawRectangle(BitmapImageX.hoverBrushRaw, pen, rect);
            }
        }

        private InnerBitmapImageX() {

        }

        public InnerBitmapImageX getClonned(BitmapImageX source) {
            InnerBitmapImageX x = new InnerBitmapImageX();
            x._source = source;
            x.ID = ID;
            x.Width = Width;
            x.Height = Height;
            x._img = _img;
            return x;
        }

        public BitmapImage Image { get { return _img; } }

        public InnerBitmapImageX(string path, BitmapImageX source) {
            _source = source;
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            string thePath = "StarcraftEPDTriggers.img." + path.Replace("/", ".");
            Stream myStream = myAssembly.GetManifestResourceStream(thePath);

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = myStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            _img = bitmap;
            Width = _img.Width;
            Height = _img.Height;
        }

    }

    public class TriggerDefinitionPartProperties {

        public RichTextBox g = new RichTextBox();

        public bool HideCheckBox = false;

        private Func<bool> _keepDef;

        public bool KeepDefault { get { return _keepDef(); } }

        public event Action<bool> SelectionChanged = (bool sel) => { };

        public void FireSelectionChange(bool selected) {
            SelectionChanged(selected);
        }

        public TriggerDefinitionPartProperties() : this(() => false) { }

        public TriggerDefinitionPartProperties(Func<bool> keepDef) {
            _keepDef = keepDef;
        }
    }

    public abstract class TriggerDefinitionPart {

        private bool _isEditable;
        private bool _isRegenerating;
        protected TriggerDefinitionPartProperties Properties;

        public bool isDefaultKept() {
            return Properties != null ? Properties.KeepDefault : false;
        }

        private Action<bool> _regenCB;

        public bool IsEditable { get { return _isEditable; } set { _isEditable = value; } }

        public bool IsRegeneratingAfterValueChange { get { return _isRegenerating; } set { _isRegenerating = value; } }

        public TriggerDefinitionPart setEditable(bool edit) {
            IsEditable = edit;
            return this;
        }

        public TriggerDefinitionPart setRegenerateAfterChange(Action<bool> regenCallback) {
            IsRegeneratingAfterValueChange = true;
            _regenCB = regenCallback;
            return this;
        }

        protected TriggerDefinitionPart() {
            _isEditable = true;
            _isRegenerating = false;
        }

        protected void ValueChanged() {
            ValueChanged(true);
        }

        public void ValueChanged(bool callCallback) {
            if (_regenCB != null) {
                if (callCallback) {
                    _regenCB(true);
                }
            }
            updateDisplayedValue();
        }

        protected abstract void updateDisplayedValue();

        protected void renderColoredText(RichTextBox g, string text, int hexColor, int hexColorSelected, bool allowNewLine) {
            Brush color = new SolidColorBrush(Color.FromRgb((byte) ((hexColor >> 16) & 0xff), (byte) ((hexColor >> 8) & 0xff), (byte) ((hexColor >> 0) & 0xff)));
            Brush colorSel = new SolidColorBrush(Color.FromRgb((byte) ((hexColorSelected >> 16) & 0xff), (byte) ((hexColorSelected >> 8) & 0xff), (byte) ((hexColorSelected >> 0) & 0xff)));

            Inline run = new Run(allowNewLine ? text : text.Replace("\r", "\\r").Replace("\n", "\\n"));
            run.BaselineAlignment = BaselineAlignment.Center;
            run.Foreground = color;
            ((Paragraph) g.Document.Blocks.LastBlock).Inlines.Add(run);
            Properties.SelectionChanged += (bool isSelected) => {
                if (isSelected) {
                    run.Foreground = colorSel;
                } else {
                    run.Foreground = color;
                }
            };
        }

        public abstract void render(TriggerDefinitionPartProperties g);

        private bool _isResetable = false;

        public bool IsResetable { get { return _isResetable; } }

        public TriggerDefinitionPart SetResetable(bool rs) {
            _isResetable = rs;
            return this;
        }

        protected UIElement GlowableElement = null;

        public virtual void glow() {
        }

        public virtual void unglow() {
        }

        internal void updatePropertiesFromAnotherSource(TriggerDefinitionPart anotherPart) {
            Properties = anotherPart.Properties;
        }
    }

    public abstract class TriggerDefinitionClickableLink : TriggerDefinitionPart {

        private InlineUIContainer _iuc;

        private bool _glowing = false;

        protected virtual void linkUnclicked(UIElement givenElement) {
            _glowing = false;
            if (givenElement != null) {
                UIElement label = getPlaceholderObject();
                _iuc.Child = label;
                glowCheck();
                label.PreviewMouseDown += delegate { linkClicked(givenElement); };
            }
        }


        private void glowCheck() {
            Brush bg;
            if (_glowing) {
                bg = new SolidColorBrush(Color.FromArgb(100, 100, 255, 100)); ;
            } else {
                bg = Brushes.Transparent;
            }
            var whatever = _iuc.Child.GetType().GetProperty("Background").GetValue(_iuc.Child);
            _iuc.Child.GetType().GetProperty("Background").SetValue(_iuc.Child, bg);
        }

        public override void unglow() {
            _glowing = false;
            glowCheck();
        }

        public override void glow() {
            GlowableElement = _iuc.Child;
            _glowing = true;
            updateDisplayedValue();
        }

        protected virtual bool CanLoseFocus(UIElement givenElement) {
            return true;
        }

        protected virtual void linkClicked(UIElement givenElement) {
            if (givenElement != null) {
                _iuc.Child = givenElement;
                _glowing = false;
                glowCheck();
                givenElement.LostFocus += delegate {
                    if (CanLoseFocus(givenElement)) {
                        linkUnclicked(givenElement);
                    }
                };
                givenElement.Focus();
            }
        }

        public override void render(TriggerDefinitionPartProperties g) {
            Properties = g;
            _iuc = new InlineUIContainer();
            updateDisplayedValue();
            ((Paragraph) g.g.Document.Blocks.LastBlock).Inlines.Add(_iuc);
        }

        protected abstract UIElement getPlaceholderObject();

        protected abstract UIElement getRealElement();

        protected override void updateDisplayedValue() {
            if (_iuc != null) { // Not rendered (in case of set deaths)
                UIElement label = getPlaceholderObject();
                GlowableElement = label;
                _iuc.BaselineAlignment = BaselineAlignment.Center;
                _iuc.Child = label;
                glowCheck();
                if (IsEditable) {
                    label.PreviewMouseDown += delegate {
                        UIElement editableElement = getRealElement();
                        linkClicked(editableElement);
                    };
                }
            }
        }
    }

    public abstract class TriggerDefinitionResetableClickableLabel : TriggerDefinitionClickableLabel {

        private Button _reset;

        public TriggerDefinitionResetableClickableLabel(Func<string> getter) : base(getter) {
        }

        protected abstract bool isCurrentValueDefault();

        protected event Action<bool> Reset = delegate { };

        protected void updateButton() {
            if (_reset != null) {
                /*
                if (Properties.KeepDefault && !isCurrentValueDefault()) {
                    Reset(true);
                    ValueChanged();
                }
                */
                _reset.Visibility = isCurrentValueDefault() ? Visibility.Collapsed : Visibility.Visible;

            }
        }

        protected override void updateDisplayedValue() {
            base.updateDisplayedValue();
            updateButton();
        }

        public override void render(TriggerDefinitionPartProperties g) {
            base.render(g);
            if (IsResetable) {
                _reset = new Button();
                _reset.Content = "Reset";
                _reset.Click += delegate {
                    Reset(true);
                };
                ((Paragraph) g.g.Document.Blocks.LastBlock).Inlines.Add(_reset);
                updateButton();
            }
        }
    }

    public abstract class TriggerDefinitionClickableLabel : TriggerDefinitionClickableLink {

        private Func<string> _getter;
        private Label _lbl;

        public TriggerDefinitionClickableLabel(Func<string> getter) {
            _getter = () => getter().Replace("_", "__");
        }

        private bool isSel = false;

        protected override void updateDisplayedValue() {
            base.updateDisplayedValue();
            if (_lbl != null) { // Not rendered ?
                _lbl.Content = _getter();
            }
        }

        protected override UIElement getPlaceholderObject() {
            Label lbl = new Label();
            if (Properties != null) {
                Properties.SelectionChanged += (bool isSelected) => {
                    isSel = isSelected;
                    if (isSelected) {
                        lbl.Foreground = Brushes.White;
                    } else {
                        lbl.Foreground = Brushes.Red;
                    }
                };
            }
            lbl.VerticalAlignment = VerticalAlignment.Center;
            lbl.VerticalContentAlignment = VerticalAlignment.Center;
            lbl.MinWidth = 10;
            lbl.Foreground = isSel ? Brushes.Wheat : Brushes.Red;
            if (IsEditable) {
                lbl.Cursor = Cursors.Hand;
            }
            lbl.Content = "'" + _getter() + "'";
            _lbl = lbl;
            return lbl;
        }

    }

    public abstract class TriggerDefinitionClickableImageWithLabel : TriggerDefinitionClickableLink {

        private Func<BitmapImageX> _getter;

        public TriggerDefinitionClickableImageWithLabel(Func<BitmapImageX> getter) {
            _getter = getter;
        }

        protected override UIElement getPlaceholderObject() {
            BitmapImageX lbl = _getter().getCloned();
            lbl.Cursor = Cursors.Hand;
            return lbl;
        }

    }

    public abstract class TriggerBasicEnumPartResetable<T> : TriggerBasicEnumPart<T> {

        private Button _reset;

        public TriggerBasicEnumPartResetable(Func<string> getter) : base(getter) { }

        protected abstract bool isCurrentValueDefault();

        protected void updateButton() {
            if (_reset != null) {
                /*
                if (Properties.KeepDefault && ! isCurrentValueDefault()) {
                    Reset(true);
                    ValueChanged();
                }
                */
                _reset.Visibility = isCurrentValueDefault() ? Visibility.Collapsed : Visibility.Visible;

            }
        }

        protected override void updateDisplayedValue() {
            base.updateDisplayedValue();
            updateButton();
        }

        protected event Action<bool> Reset = delegate { };

        public override void render(TriggerDefinitionPartProperties g) {
            base.render(g);
            if (IsResetable) {
                _reset = new Button();
                _reset.Content = "Reset";
                _reset.Click += delegate {
                    Reset(true);
                };
                ((Paragraph) g.g.Document.Blocks.LastBlock).Inlines.Add(_reset);
                updateButton();
            }
        }
    }

    public abstract class TriggerBasicEnumPart<T> : TriggerDefinitionClickableLabel {

        private Func<T> _getter;
        private Action<T> _setter;
        private T[] givenArray;

        public T[] getGivenObjectArray() {
            return givenArray;
        }

        protected override void linkUnclicked(UIElement givenElement) {
            ComboBox combo = givenElement as ComboBox;
            if (combo.SelectedIndex >= 0) {
                T selectedItem = (T) combo.SelectedItem;
                _setter(selectedItem);
                ValueChanged();
            }
            base.linkUnclicked(givenElement);
        }


        public TriggerBasicEnumPart(Func<string> getter) : base(getter) { }

        public override string ToString() {
            return _getter().ToString();
        }

        protected void setup(Func<T> getter, Action<T> setter, T[] values) {
            givenArray = values;
            _getter = getter;
            _setter = (T val) => { setter(val); ValueChanged(); };
        }

        protected override bool CanLoseFocus(UIElement givenElement) {
            return false;
        }

        protected override UIElement getRealElement() {
            ComboBox combo = new ComboBox();
            foreach (T obj in givenArray) {
                combo.Items.Add(obj);
            }
            combo.IsReadOnly = false;
            combo.IsDropDownOpen = true;
            combo.StaysOpenOnEdit = true;
            combo.IsEditable = true;
            combo.SelectedItem = _getter();
            combo.KeyDown += (object sender, KeyEventArgs args) => {
                if (args.Key == Key.Return || args.Key == Key.Enter) {
                    args.Handled = true;
                } else if (args.Key == Key.Escape) {
                    args.Handled = true;
                }
            };
            combo.Loaded += delegate {
                TextBox tbb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
                Popup pu = combo.Template.FindName("PART_Popup", combo) as Popup;
                pu.Closed += delegate {
                    linkUnclicked(combo);
                };
                tbb.KeyDown += (object sender, KeyEventArgs args) => {
                    if (args.Key == Key.Return || args.Key == Key.Enter) {
                        args.Handled = true;
                    } else if (args.Key == Key.Escape) {
                        args.Handled = true;
                    }
                };
                tbb.Focusable = true;
                pu.Focusable = true;
                pu.Opened += delegate {
                    tbb.Focus();
                    tbb.SelectAll();
                };
                pu.IsOpen = true;
            };
            combo.Focusable = true;
            return combo;
        }
    }

    public class TriggerDefinitionLabel : TriggerDefinitionPart, SaveableItem {

        private string _text;
        private int _color;

        public TriggerDefinitionLabel(string text) : this(text, 0) { }

        public TriggerDefinitionLabel(string text, int color) {
            _text = text;
            _color = color;
        }

        public override void render(TriggerDefinitionPartProperties g) {
            Properties = g;
            renderColoredText(g.g, _text, _color, _color, true);
        }

        public string ToSaveString(bool readable) {
            throw new NotImplementedException();
        }

        public SaveableItem getDefaultValue() {
            throw new NotImplementedException();
        }

        protected override void updateDisplayedValue() {

        }
    }

    public class TriggerDefinitionNewLine : TriggerDefinitionPart {

        public override void render(TriggerDefinitionPartProperties g) {
            Properties = g;
            renderColoredText(g.g, "\n", 0, 0, true);
        }

        protected override void updateDisplayedValue() {

        }
    }

    public class TriggerDefinitionParagraphStart : TriggerDefinitionPart {

        private static Brush[] backgrounds = new Brush[] { new SolidColorBrush(Color.FromRgb(240, 240, 240)), new SolidColorBrush(Color.FromRgb(220, 220, 220)) };
        private static Brush[] backgroundsSel = new Brush[] { new SolidColorBrush(Color.FromArgb(100, 240, 240, 240)), new SolidColorBrush(Color.FromArgb(100, 220, 220, 220)) };

        public override void render(TriggerDefinitionPartProperties g) {
            Properties = g;
            Paragraph paragraph = new Paragraph();
            Brush background = backgrounds[g.g.Document.Blocks.Count % 2];
            Brush backgroundSel = backgroundsSel[g.g.Document.Blocks.Count % 2];
            paragraph.Background = background;

            paragraph.Margin = new Thickness(50, 0, 0, 0);
            paragraph.LineHeight = 1;
            g.g.Document.Blocks.Add(paragraph);
            Properties.SelectionChanged += (bool isSelected) => {
                if (isSelected) {
                    paragraph.Background = backgroundSel;
                } else {
                    paragraph.Background = background;
                }
            };
        }

        protected override void updateDisplayedValue() {

        }
    }

    public class TriggerDefinitionParagraphEnd : TriggerDefinitionPart {

        public override void render(TriggerDefinitionPartProperties g) {
            Properties = g;
            Paragraph paragraph = new Paragraph();
            paragraph.Margin = new Thickness(0, 0, 0, 0);
            paragraph.LineHeight = 1;
            g.g.Document.Blocks.Add(paragraph);
        }

        protected override void updateDisplayedValue() {

        }
    }

    public class TriggerDefinitionCheckbox : TriggerDefinitionPart {

        private Func<bool> _getter;
        private Action<bool> _setter;
        private string _text;
        private CheckBox _cb;

        protected bool overridePropertiesHideCheckbox = false;

        public override void render(TriggerDefinitionPartProperties g) {
            Properties = g;
            if (Properties.HideCheckBox && !overridePropertiesHideCheckbox) {
                return;
            }
            CheckBox cb = new CheckBox();
            cb.Content = _text;
            cb.Checked += delegate { _setter(true); ValueChanged(); };
            cb.Unchecked += delegate { _setter(false); ValueChanged(); };
            cb.IsChecked = _getter();
            cb.Margin = new Thickness(5, 0, 0, 0);
            cb.VerticalAlignment = VerticalAlignment.Center;
            cb.VerticalContentAlignment = VerticalAlignment.Center;
            cb.Height = 24;
            ((Paragraph) g.g.Document.Blocks.LastBlock).Inlines.Add(cb);
            _cb = cb;
        }

        protected override void updateDisplayedValue() {
            if (_cb != null) {
                _cb.IsChecked = _getter();
            }
        }

        public TriggerDefinitionCheckbox(Func<bool> getter, Action<bool> setter, string text) {
            _getter = getter;
            _setter = setter;
            _text = text;
        }
    }

    public class TriggerDefinitionResetableCheckbox : TriggerDefinitionCheckbox {

        private Button _reset;

        private Func<bool> _getter;
        private Func<bool> _getDefault;
        private Action<bool> _setter;

        public TriggerDefinitionResetableCheckbox(Func<bool> getter, Action<bool> setter, Func<bool> getDefault) : base(getter, setter, "") {
            _getter = getter;
            _setter = setter;
            _getDefault = getDefault;
            overridePropertiesHideCheckbox = true;
        }

        protected bool isCurrentValueDefault() {
            return _getter() == _getDefault();
        }

        protected void updateButton() {
            if (_reset != null) {
                /*
                if (Properties.KeepDefault && !isCurrentValueDefault()) {
                    _setter(_getDefault());
                    updateDisplayedValue();
                }
                */
                _reset.Visibility = isCurrentValueDefault() ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        protected override void updateDisplayedValue() {
            base.updateDisplayedValue();
            updateButton();
        }

        public override void render(TriggerDefinitionPartProperties g) {
            base.render(g);
            if (IsResetable) {
                _reset = new Button();
                _reset.Content = "Reset";
                _reset.Click += delegate {
                    _setter(_getDefault());
                    updateDisplayedValue();
                };
                ((Paragraph) g.g.Document.Blocks.LastBlock).Inlines.Add(_reset);
                updateButton();
            }
        }

    }

    /*****************************************************************\
    *                                                                 *
    *                                                                 *
    *               Warning: Bullshit section ahead!                  *
    *                                                                 *
    *                                                                 *
    \*****************************************************************/


    public abstract class TriggerDefinitionResetableImageList : TriggerDefinitionClickableImageWithLabel {

        private Func<BitmapImageX> _getter;
        private Action<BitmapImageX> _setter;
        private Func<BitmapImageX> _getDefault;
        private BitmapImageX[] _images;


        protected int ElementsPerColumn = 10;
        private Button _reset;

        public BitmapImageX[] getGivenObjectArray() {
            return _images;
        }

        public override void render(TriggerDefinitionPartProperties g) {
            base.render(g);
            if (IsResetable) {
                _reset = new Button();
                _reset.Content = "Reset";
                _reset.Click += delegate {
                    _setter(_getDefault());
                    ValueChanged();
                    updateButton();
                };
                ((Paragraph) g.g.Document.Blocks.LastBlock).Inlines.Add(_reset);
                updateButton();
            }
        }

        protected abstract bool isCurrentValueDefault();

        protected void updateButton() {
            if (_reset != null) {
                /*
                if (Properties.KeepDefault && !isCurrentValueDefault()) {
                    _setter(_getDefault());
                    updateDisplayedValue();
                }
                */
                _reset.Visibility = isCurrentValueDefault() ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        protected override void updateDisplayedValue() {
            base.updateDisplayedValue();
            updateButton();
        }

        public TriggerDefinitionResetableImageList(Func<BitmapImageX> getter) : base(getter) { }

        protected void setup(Func<BitmapImageX> getter, Action<BitmapImageX> setter, Func<BitmapImageX> getDefault, BitmapImageX[] images) {
            _getter = getter;
            _setter = setter;
            _images = images;
            _getDefault = getDefault;
        }

        protected override bool CanLoseFocus(UIElement givenElement) {
            return false;
        }

        protected override UIElement getRealElement() {
            ComboBox combo = new ComboBox();
            /*
            foreach (BitmapImageX obj in _images) {
                combo.Items.Add(obj);
            }*/
            combo.IsReadOnly = true;
            combo.IsDropDownOpen = true;
            combo.StaysOpenOnEdit = true;
            combo.IsEditable = true;
            combo.SelectedItem = _getter();
            combo.Loaded += delegate {
                TextBox tbb = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
                Popup pu = combo.Template.FindName("PART_Popup", combo) as Popup;
                MyIconGridPanel grd = new MyIconGridPanel(_images, _getter(), (BitmapImageX img) => { pu.IsOpen = false; _setter(img); }, ElementsPerColumn);
                pu.Child = grd;
                pu.Closed += delegate {
                    linkUnclicked(combo);
                };
                pu.Opened += delegate {
                    grd.Focus();
                };
                tbb.Focusable = true;
                pu.Focusable = true;
                tbb.SelectAll();
                pu.IsOpen = true;
            };
            combo.Focusable = true;
            return combo;
        }
    }

    public class TriggerDefinitionGeneralIconsList<TYPE> : TriggerDefinitionResetableImageList where TYPE : Gettable<TYPE>, GettableImage {

        private Func<TYPE> _getDefault;
        private Func<TYPE> _getter;
        private Action<TYPE> _setter;

        protected override bool isCurrentValueDefault() {
            return _getter().getIndex() == _getDefault().getIndex();
        }

        public TriggerDefinitionGeneralIconsList(Func<TYPE> getter, Action<TYPE> setter, Func<TYPE> getDefault, BitmapImageX[] allImages, int columns) : base(() => getter().getImage()) {
            _setter = (TYPE valu) => { setter(valu); ValueChanged(); updateButton(); };
            _getter = getter;
            _getDefault = getDefault;
            setup(() => _getter().getImage(), (BitmapImageX img) => { _setter((TYPE) img.GetValue()); }, () => _getDefault().getImage(), allImages);
            ElementsPerColumn = columns;
        }
    }

    public class TriggerDefinitionPropertiesDef : TriggerDefinitionClickableLabel {

        private Func<PropertiesDef> _getter;
        private Action<PropertiesDef> _setter;

        protected override void linkClicked(UIElement editableElement) {
            new WndUnitProperties(this);
        }

        public TriggerDefinitionPropertiesDef(Func<PropertiesDef> getter, Action<PropertiesDef> setter) : base(() => getter().ToString()) {
            _getter = getter;
            _setter = setter;
        }

        protected override UIElement getRealElement() {
            return null;
        }

        internal void set(object result) {
            if (result == null) {
                return;
            } else {
                _setter(new PropertiesDef((int) result));
                ValueChanged();
            }
        }
    }

    public class TriggerDefinitionWeaponTargetFlagsDef : TriggerDefinitionResetableClickableLabel {

        private Func<WeaponTargetFlags> _getter;
        private Action<WeaponTargetFlags> _setter;
        private Func<WeaponTargetFlags> _getDefault;


        public TriggerDefinitionWeaponTargetFlagsDef(Func<WeaponTargetFlags> getter, Action<WeaponTargetFlags> setter, Func<WeaponTargetFlags> getDefault) : base(() => getter().ToString()) {
            _getter = getter;
            _setter = (WeaponTargetFlags def) => { setter(def); ValueChanged(); updateButton(); };
            _getDefault = getDefault;
        }

        protected override UIElement getRealElement() {
            return null;
        }

        protected override void linkClicked(UIElement editableElement) {
            new WndWeaponTargetFlags(_getter, _setter, _getDefault());
        }

        protected override bool isCurrentValueDefault() {
            return _getter().getIndex() == _getDefault().getIndex();
        }
    }

    public class TriggerDefinitionAdvancedPropertiesDef : TriggerDefinitionResetableClickableLabel {

        private Func<AdvancedPropertiesDef> _getter;
        private Action<AdvancedPropertiesDef> _setter;
        private Func<AdvancedPropertiesDef> _getDefault;


        public TriggerDefinitionAdvancedPropertiesDef(Func<AdvancedPropertiesDef> getter, Action<AdvancedPropertiesDef> setter, Func<AdvancedPropertiesDef> getDefault) : base(() => getter().ToString()) {
            _getter = getter;
            _setter = (AdvancedPropertiesDef def) => { setter(def); ValueChanged(); updateButton(); };
            _getDefault = getDefault;
        }

        protected override UIElement getRealElement() {
            return null;
        }

        protected override void linkClicked(UIElement editableElement) {
            new WndAdvancedUnitProperties(_getter, _setter, _getDefault().getIndex());
        }

        protected override bool isCurrentValueDefault() {
            return _getter().getIndex() == _getDefault().getIndex();
        }
    }

    public class TriggerDefinitionQuantAmount : TriggerDefinitionClickableLabel {

        private Func<UnitsQuantity> _getter;
        private Action<UnitsQuantity> _setter;

        public TriggerDefinitionQuantAmount(Func<UnitsQuantity> getter, Action<UnitsQuantity> setter) : base(() => getter().Amount) {
            _getter = getter;
            _setter = (UnitsQuantity val) => { setter(val); ValueChanged(); };
        }

        protected override void linkClicked(UIElement givenElement) {
            new WndPlayerQuant(this, _getter().RawAmount);
        }

        internal void set(object result) {
            if (result == null) { // Cancel clicked
                return;
            } else {
                int value = (int) result;
                _setter(new UnitsQuantity(value));
            }
        }

        protected override UIElement getRealElement() {
            return null;
        }
    }

    public class TriggerDefinitionPercentage : TriggerDefinitionClickableLabel {

        private Func<PercentageDef> _getter;
        private Action<PercentageDef> _setter;

        public TriggerDefinitionPercentage(Func<PercentageDef> getter, Action<PercentageDef> setter) : base(() => getter().ToString()) {
            _getter = getter;
            _setter = (PercentageDef val) => { setter(val); ValueChanged(); };
        }

        protected override UIElement getRealElement() {
            TextBox tb = new TextBox();
            tb.Text = _getter().ToString();
            return tb;
        }
    }

    public class TriggerDefinitionInputText : TriggerDefinitionClickableLabel {

        private Func<StringDef> _getter;
        private Action<StringDef> _setter;

        protected override void linkClicked(UIElement editableElement) {
            new WndStringPropertyEdit(() => _getter().ToString(), () => true, (string str, bool b) => { _setter(new StringDef(str)); });
        }

        public TriggerDefinitionInputText(Func<StringDef> getter, Action<StringDef> setter) : base(() => { return getter().ToString().Length == 0 ? "   " : getter().ToString(); }) {
            _getter = getter;
            _setter = (StringDef val) => { setter(val); ValueChanged(); };
        }
        protected override UIElement getRealElement() {
            return null;
        }
    }



    /*****************************************************************\
    *                                                                 *
    *                                                                 *
    *            Warning: Easier bullshit section ahead!              *
    *                                                                 *
    *                                                                 *
    \*****************************************************************/

    public class TriggerDefinitionGeneralDef<TYPE> : TriggerBasicEnumPartResetable<TYPE> where TYPE : Gettable<TYPE> {

        private Func<TYPE> _getter;
        private Func<TYPE> _getDefault;
        private Action<TYPE> _setter;

        public TriggerDefinitionGeneralDef(Func<TYPE> getter, Action<TYPE> setter, Func<TYPE> defaultValue, TYPE[] allValues) : base(() => getter().ToString()) {
            _getDefault = defaultValue;
            _getter = getter;
            _setter = (TYPE val) => { setter(val); ValueChanged(); updateButton(); };
            Reset += delegate {
                _setter(defaultValue());
            };
            setup(getter, _setter, allValues);
        }

        protected override bool isCurrentValueDefault() {
            return _getter().getIndex() == _getDefault().getIndex();
        }
    }

    public class TriggerDefinitionIntAmount : TriggerDefinitionResetableClickableLabel {

        private static void verifyAndSet(StringDef input, Action<IntDef> _setter) {
            string str = input.ToString();
            if (str.Length > 2) {
                if (str[0] == '0' && str[1] == 'x') {
                    int numhex;
                    if (int.TryParse(str.Substring(2), NumberStyles.HexNumber, null, out numhex)) {
                        IntDef def = new IntDef(numhex, true);
                        _setter(def);
                    }
                }
            }


            int num;
            if (int.TryParse(input.ToString(), out num)) {
                IntDef def = new IntDef(num, false);
                _setter(def);
            }
        }

        private Func<IntDef> _getter;
        private Action<IntDef> _setter;
        private Func<IntDef> _getDefault;

        public TriggerDefinitionIntAmount(Func<IntDef> getter, Action<IntDef> setter, Func<IntDef> getDefault) : base(() => { return getter().ToString().Length == 0 ? "   " : getter().ToString(); }) {
            _getter = getter;
            _getDefault = getDefault;
            _setter = (IntDef val) => { bool useHex = val.UseHex; setter(val); getter().UseHex = useHex; ValueChanged(); updateButton(); };
            Reset += delegate {
                _setter(getDefault());
            };
        }

        protected override void linkUnclicked(UIElement givenElement) {
            verifyAndSet(new StringDef(((TextBox) givenElement).Text), _setter);
            base.linkUnclicked(givenElement);
        }

        protected override void linkClicked(UIElement givenElement) {
            base.linkClicked(givenElement);
            TextBox tb = givenElement as TextBox;
            tb.SelectAll();
            tb.Focus();
        }

        protected override UIElement getRealElement() {
            TextBox tb = new TextBox();
            tb.MinWidth = 50;
            IntDef originalValue = _getter();
            tb.KeyDown += (object sender, KeyEventArgs args) => {
                if (args.Key == Key.Return || args.Key == Key.Enter) {
                    args.Handled = true;
                    linkUnclicked(tb);
                } else if (args.Key == Key.Escape) {
                    args.Handled = true;
                    tb.Text = originalValue.ToString();
                    linkUnclicked(tb);
                }
            };
            tb.Loaded += delegate {
                tb.SelectAll();
                tb.Focus();
            };
            tb.Text = originalValue.ToString();
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Padding = new Thickness(5, 5, 5, 5);
            tb.IsReadOnly = false;
            return tb;
        }

        protected override bool isCurrentValueDefault() {
            return _getDefault().getIndex() == _getter().getIndex();
        }

        public int getValue() {
            return _getter().getIndex();
        }

        public void setValue(int v) {
            _setter(new IntDef(v, false));
        }
    }

    public abstract class TriggerContent {

        private bool enabled = true;

        public bool isEnabled() {
            return enabled;
        }

        public void setEnabled(bool enabled) {
            this.enabled = enabled;
        }

        protected abstract TriggerDefinitionPart[] getInnerDefinitionParts();

        public List<TriggerDefinitionPart> getDefinitionParts() {
            List<TriggerDefinitionPart> lst = new List<TriggerDefinitionPart>();
            lst.Add(new TriggerDefinitionCheckbox(() => enabled, (bool en) => { enabled = en; }, " "));
            lst.AddRange(getInnerDefinitionParts());
            return lst;
        }

        private Token[] tokens;

        protected TriggerContent(Parser sc, int num) {
            if (sc != null) { // EPD and EUD do not have parsers, they are generated by DEATH actions
                tokens = new Token[num];
                if (!(sc.getNextToken() is LeftBracket)) {
                    throw new NotImplementedException();
                }
                for (int i = 0; i < num; i++) {
                    tokens[i] = sc.getNextToken();
                    if (i + 1 < num) {
                        Token t = sc.getNextToken();
                        if (!(t is Comma)) {
                            throw new NotImplementedException();
                        }
                    }
                }
                if (!(sc.getNextToken() is RightBracket)) {
                    throw new NotImplementedException();
                }
            }
        }

        public EnableState getEnableState(int index) {
            return tokens[index - 1].toEnableState();
        }

        public SwitchSetState getSwitchSetState(int index) {
            return tokens[index - 1].toSwitchSetState();
        }

        public MessageType getMessageType(int index) {
            return tokens[index - 1].getMessageType();
        }

        public StringDef getString(int index) {
            return tokens[index - 1].toStringDef();
        }

        public IntDef getInt(int index, bool usehex) {
            return IntDef.getByIndex(tokens[index - 1].toInt(), usehex);
        }

        public SwitchNameDef getSwitch(int index) {
            return SwitchNameDef.getByIndex(tokens[index - 1].toSwitchInt());
        }

        public AIScriptDef getAIScript(int index) {
            return AIScriptDef.getByStringValue(getString(index).ToString());
        }

        public AIScriptAtDef getAIScriptAt(int index) {
            return AIScriptAtDef.getByStringValue(getString(index).ToString());
        }

        public Quantifier getQuantifier(int index) {
            return tokens[index - 1].toQuantifier();
        }

        public Order getOrder(int index) {
            return tokens[index - 1].toOrder();
        }

        public LocationDef getLocationDef(int index) {
            return tokens[index - 1].toLocationDef();
        }

        public SetQuantifier getSetQuantifier(int index) {
            return tokens[index - 1].toSetQuantifier();
        }

        public PlayerDef getPlayerDef(int index) {
            return tokens[index - 1].toPlayerDef();
        }

        public Resources getResources(int index) {
            return tokens[index - 1].toResources();
        }

        public ScoreBoard getScoreBoard(int index) {
            return tokens[index - 1].toScoreBoard();
        }

        public AllianceDef getAlliance(int index) {
            return tokens[index - 1].toAlliance();
        }

        public SwitchState getSwitchState(int index) {
            return tokens[index - 1].toSwitchState();
        }

        public UnitVanillaDef getUnitDef(int index) {
            return UnitVanillaDef.getByIndex(tokens[index - 1].toUnitInt());
        }
    }

    public class TriggerContentType {

        public class TriggerContentTypeDescriptor {

            private string _name;
            private Func<TriggerContent, int, SaveableItem> _reader;
            private Func<Func<SaveableItem>, Action<SaveableItem>, TriggerDefinitionPart> _partConstructor;
            private SaveableItem _defaultValue;


            public TriggerContentTypeDescriptor(string name, Func<TriggerContent, int, SaveableItem> reader, Func<Func<SaveableItem>, Action<SaveableItem>, TriggerDefinitionPart> partConstructor, SaveableItem defaultValue) {
                _name = name;
                _reader = reader;
                _partConstructor = partConstructor;
                _defaultValue = defaultValue;
            }

            public virtual SaveableItem Read(TriggerContent parser, int index) {
                return _reader(parser, index + 1);
            }

            public virtual TriggerDefinitionPart GetDefinitionPart(Func<SaveableItem> getter, Action<SaveableItem> setter) {
                return _partConstructor(getter, setter);
            }

            internal SaveableItem getDefaultValue() {
                return _defaultValue;
            }

            public override string ToString() {
                return "Field definition for " + _name;
            }
        }

        public class TriggerContentTypeDescriptorVisual : TriggerContentTypeDescriptor {

            private string _name;

            public string Content { get { return _name; } }

            public TriggerContentTypeDescriptorVisual(string name) : base(name, null, null, null) {
                _name = name;
            }

            public override SaveableItem Read(TriggerContent parser, int index) {
                return new TriggerDefinitionLabel(_name);
            }

            public override TriggerDefinitionPart GetDefinitionPart(Func<SaveableItem> getter, Action<SaveableItem> setter) {
                return new TriggerDefinitionLabel(_name);
            }

            public override string ToString() {
                return "Visual placeholder for " + _name;
            }
        }

        private static TriggerContentTypeDescriptor _get(string name, Func<TriggerContent, int, SaveableItem> reader, Func<Func<SaveableItem>, Action<SaveableItem>, TriggerDefinitionPart> partConstructor, SaveableItem defaultValue) {
            return new TriggerContentTypeDescriptor(name, reader, partConstructor, defaultValue);
        }


        private static Func<string, TriggerContentTypeDescriptor> UnitsQuantity = (string name) => _get(name, (TriggerContent contents, int index) => new UnitsQuantity(contents.getInt(index, false).getIndex()), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionQuantAmount(() => (UnitsQuantity) getter(), (UnitsQuantity obj) => { setter(obj); }), StarcraftEPDTriggers.UnitsQuantity.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Unit = (string name) => _get(name, (TriggerContent contents, int index) => contents.getUnitDef(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<UnitVanillaDef>(() => (UnitVanillaDef) getter(), (UnitVanillaDef obj) => { setter(obj); }, UnitVanillaDef.getDefaultValue, UnitVanillaDef.AllUnits), UnitVanillaDef.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Location = (string name) => _get(name, (TriggerContent contents, int index) => contents.getLocationDef(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<LocationDef>(() => (LocationDef) getter(), (LocationDef obj) => { setter(obj); }, LocationDef.getDefaultValue, LocationDef.AllLocations), LocationDef.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Player = (string name) => _get(name, (TriggerContent contents, int index) => contents.getPlayerDef(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<PlayerDef>(() => (PlayerDef) getter(), (PlayerDef obj) => { setter(obj); }, PlayerDef.getDefaultValue, PlayerDef.AllPlayers), PlayerDef.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Properties = (string name) => _get(name, (TriggerContent contents, int index) => new PropertiesDef(contents.getInt(index, false).getIndex()), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionPropertiesDef(() => (PropertiesDef) getter(), (PropertiesDef obj) => { setter(obj); }), StarcraftEPDTriggers.PropertiesDef.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Percentage = (string name) => _get(name, (TriggerContent contents, int index) => new PercentageDef(contents.getInt(index, false).getIndex()), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionPercentage(() => (PercentageDef) getter(), (PercentageDef obj) => { setter(obj); }), StarcraftEPDTriggers.PercentageDef.getDefaultValue(false));
        private static Func<string, TriggerContentTypeDescriptor> String = (string name) => _get(name, (TriggerContent contents, int index) => contents.getString(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionInputText(() => (StringDef) getter(), (StringDef obj) => { setter(obj); }), new StringDef(name));
        private static Func<string, TriggerContentTypeDescriptor> Int = (string name) => _get(name, (TriggerContent contents, int index) => contents.getInt(index, false), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionIntAmount(() => (IntDef) getter(), (IntDef obj) => { setter(obj); }, () => new IntDef(0, false)), IntDef.getDefaultValue(false));

        private static Func<string, TriggerContentTypeDescriptor> Alliance = (string name) => _get(name, (TriggerContent contents, int index) => contents.getAlliance(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<AllianceDef>(() => (StarcraftEPDTriggers.AllianceDef) getter(), (StarcraftEPDTriggers.AllianceDef toSet) => { setter(toSet); }, StarcraftEPDTriggers.AllianceDef.getDefaultValue, StarcraftEPDTriggers.AllianceDef.AllAlliances), StarcraftEPDTriggers.AllianceDef.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> SwitchSetState = (string name) => _get(name, (TriggerContent contents, int index) => contents.getSwitchSetState(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<SwitchSetState>(() => (StarcraftEPDTriggers.SwitchSetState) getter(), (StarcraftEPDTriggers.SwitchSetState toSet) => { setter(toSet); }, StarcraftEPDTriggers.SwitchSetState.getDefaultValue, StarcraftEPDTriggers.SwitchSetState.AllStates), StarcraftEPDTriggers.SwitchSetState.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> EnableState = (string name) => _get(name, (TriggerContent contents, int index) => contents.getEnableState(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<EnableState>(() => (StarcraftEPDTriggers.EnableState) getter(), (StarcraftEPDTriggers.EnableState toSet) => { setter(toSet); }, StarcraftEPDTriggers.EnableState.getDefaultValue, StarcraftEPDTriggers.EnableState.AllStates), StarcraftEPDTriggers.EnableState.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> SetQuantifier = (string name) => _get(name, (TriggerContent contents, int index) => contents.getSetQuantifier(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<SetQuantifier>(() => (StarcraftEPDTriggers.SetQuantifier) getter(), (StarcraftEPDTriggers.SetQuantifier toSet) => { setter(toSet); }, StarcraftEPDTriggers.SetQuantifier.getDefaultValue, StarcraftEPDTriggers.SetQuantifier.AllQuantifiers), StarcraftEPDTriggers.SetQuantifier.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Order = (string name) => _get(name, (TriggerContent contents, int index) => contents.getOrder(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<Order>(() => (StarcraftEPDTriggers.Order) getter(), (StarcraftEPDTriggers.Order toSet) => { setter(toSet); }, StarcraftEPDTriggers.Order.getDefaultValue, StarcraftEPDTriggers.Order.AllOrders), StarcraftEPDTriggers.Order.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> MessageType = (string name) => _get(name, (TriggerContent contents, int index) => contents.getMessageType(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<MessageType>(() => (StarcraftEPDTriggers.MessageType) getter(), (StarcraftEPDTriggers.MessageType toSet) => { setter(toSet); }, StarcraftEPDTriggers.MessageType.getDefaultValue, StarcraftEPDTriggers.MessageType.AllDisplays), StarcraftEPDTriggers.MessageType.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> SwitchState = (string name) => _get(name, (TriggerContent contents, int index) => contents.getSwitchState(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<SwitchState>(() => (StarcraftEPDTriggers.SwitchState) getter(), (StarcraftEPDTriggers.SwitchState toSet) => { setter(toSet); }, StarcraftEPDTriggers.SwitchState.getDefaultValue, StarcraftEPDTriggers.SwitchState.AllStates), StarcraftEPDTriggers.SwitchState.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Quantifier = (string name) => _get(name, (TriggerContent contents, int index) => contents.getQuantifier(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<Quantifier>(() => (StarcraftEPDTriggers.Quantifier) getter(), (StarcraftEPDTriggers.Quantifier toSet) => { setter(toSet); }, StarcraftEPDTriggers.Quantifier.getDefaultValue, StarcraftEPDTriggers.Quantifier.AllQuantifieres), StarcraftEPDTriggers.Quantifier.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Resources = (string name) => _get(name, (TriggerContent contents, int index) => contents.getResources(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<Resources>(() => (StarcraftEPDTriggers.Resources) getter(), (StarcraftEPDTriggers.Resources toSet) => { setter(toSet); }, StarcraftEPDTriggers.Resources.getDefaultValue, StarcraftEPDTriggers.Resources.AllResources), StarcraftEPDTriggers.Resources.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> ScoreBoard = (string name) => _get(name, (TriggerContent contents, int index) => contents.getScoreBoard(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<ScoreBoard>(() => (StarcraftEPDTriggers.ScoreBoard) getter(), (StarcraftEPDTriggers.ScoreBoard toSet) => { setter(toSet); }, StarcraftEPDTriggers.ScoreBoard.getDefaultValue, StarcraftEPDTriggers.ScoreBoard.AllScoreBoards), StarcraftEPDTriggers.ScoreBoard.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> Address = (string name) => _get(name, (TriggerContent contents, int index) => contents.getInt(index, true), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionIntAmount(() => { IntDef val = (IntDef) getter(); val.UseHex = true; return val; }, (IntDef obj) => { setter(obj); }, () => new IntDef(0, true)), StarcraftEPDTriggers.IntDef.getDefaultValue(true));

        private static Func<string, TriggerContentTypeDescriptor> AIScript = (string name) => _get(name, (TriggerContent contents, int index) => contents.getAIScript(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<AIScriptDef>(() => (AIScriptDef) getter(), (AIScriptDef obj) => { setter(obj); }, StarcraftEPDTriggers.AIScriptDef.getDefaultValue, StarcraftEPDTriggers.AIScriptDef.AllScripts), StarcraftEPDTriggers.AIScriptDef.getDefaultValue());
        private static Func<string, TriggerContentTypeDescriptor> AIScriptAt = (string name) => _get(name, (TriggerContent contents, int index) => contents.getAIScriptAt(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<AIScriptAtDef>(() => (AIScriptAtDef) getter(), (AIScriptAtDef obj) => { setter(obj); }, StarcraftEPDTriggers.AIScriptAtDef.getDefaultValue, StarcraftEPDTriggers.AIScriptAtDef.AllScripts), StarcraftEPDTriggers.AIScriptAtDef.getDefaultValue());

        private static Func<string, TriggerContentTypeDescriptor> SwitchName = (string name) => _get(name, (TriggerContent contents, int index) => contents.getSwitch(index), (Func<SaveableItem> getter, Action<SaveableItem> setter) => new TriggerDefinitionGeneralDef<SwitchNameDef>(() => (SwitchNameDef) getter(), (SwitchNameDef obj) => { setter(obj); }, StarcraftEPDTriggers.SwitchNameDef.getDefaultValue, StarcraftEPDTriggers.SwitchNameDef.AllSwitches), StarcraftEPDTriggers.SwitchNameDef.getDefaultValue());


        public static TriggerContentTypeDescriptor VISUAL_LABEL(string name) {
            return new TriggerContentTypeDescriptorVisual(name);
        }




        public static TriggerContentTypeDescriptor ALLIANCE = Alliance("Alliance");
        public static TriggerContentTypeDescriptor SWITCH_SET_STATE = SwitchSetState("Switch set state");

        public static TriggerContentTypeDescriptor ENABLE_STATE = EnableState("Enable state");
        public static TriggerContentTypeDescriptor SET_QUANTIFIER = SetQuantifier("Set quantifier");
        public static TriggerContentTypeDescriptor ORDER = Order("Order type");
        public static TriggerContentTypeDescriptor MESSAGE_TYPE = MessageType("Message type");
        public static TriggerContentTypeDescriptor MESSAGE = String("Message");
        public static TriggerContentTypeDescriptor SWITCH_STATE = SwitchState("Switch state");
        public static TriggerContentTypeDescriptor QUANTIFIER = Quantifier("Quantifier");
        public static TriggerContentTypeDescriptor UNITS_QUANTITY = UnitsQuantity("Units quantity");
        public static TriggerContentTypeDescriptor RESOURCES = Resources("Resources");
        public static TriggerContentTypeDescriptor SCOREBOARD = ScoreBoard("Score board");
        public static TriggerContentTypeDescriptor UNIT_TYPE = Unit("Unit type");
        public static TriggerContentTypeDescriptor LOCATION = Location("Location");

        public static TriggerContentTypeDescriptor SOURCE_LOCATION = Location("Source location");
        public static TriggerContentTypeDescriptor TARGET_LOCATION = Location("Target location");

        public static TriggerContentTypeDescriptor PLAYER = Player("Player");
        public static TriggerContentTypeDescriptor SOURCE_PLAYER = Player("Source player");
        public static TriggerContentTypeDescriptor TARGET_PLAYER = Player("Target player");

        public static TriggerContentTypeDescriptor PROPERTIES = Properties("Properties");
        public static TriggerContentTypeDescriptor PERCENTAGE = Percentage("Percentage");
        public static TriggerContentTypeDescriptor TITLE = String("Title");
        public static TriggerContentTypeDescriptor COMMENT = String("Comment");

        public static TriggerContentTypeDescriptor WAVPATH = String("Wav path");
        public static TriggerContentTypeDescriptor WAVPATH_PARAM = Int("param");
        public static TriggerContentTypeDescriptor AMOUNT = Int("Amount");
        public static TriggerContentTypeDescriptor TIMEOUT = Int("Timeout");
        public static TriggerContentTypeDescriptor ADDRESS = Address("Address");

        public static TriggerContentTypeDescriptor AISCRIPT = AIScript("AI Script");

        public static TriggerContentTypeDescriptor AISCRIPTAT = AIScriptAt("AI Script");



        public static TriggerContentTypeDescriptor SWITCH_NAME = SwitchName("Switch Name");

        public static TriggerContentTypeDescriptor MEMORY_ADDRESS = Int("Memory address");
    }
}
