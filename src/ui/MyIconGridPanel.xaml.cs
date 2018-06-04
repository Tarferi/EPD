using System;
using System.Windows;
using System.Windows.Controls;

namespace StarcraftEPDTriggers.src.ui {

    public partial class MyIconGridPanel : UserControl {

        public MyIconGridPanel(BitmapImageX[] images, BitmapImageX selected, Action<BitmapImageX> clicked, int elementsPerLine) {
            InitializeComponent();
            wrp.IsHitTestVisible = true;
            foreach(BitmapImageX img in images) {
                BitmapImageX imgc = img.getCloned();
                if(selected == img) {
                    imgc.IsSelected = true;
                    imgc.Loaded += delegate {
                        Focus();
                        int top = (int) imgc.Margin.Top;
                        scroll.ScrollToVerticalOffset(top);
                    };
                }
                imgc.IsHitTestVisible = true;
                imgc.PreviewMouseLeftButtonDown += delegate {
                    clicked(img);
                };
                imgc.MouseEnter += delegate {
                    imgc.Hover = true;
                };
                imgc.MouseLeave += delegate {
                    imgc.Hover = false;
                };
                wrp.Children.Add(imgc);
            }
            Thickness padding = new Thickness(5, 5, 5, 5);
            Loaded += delegate {
                using (var d = Dispatcher.DisableProcessing()) {
                    int maxWidth = 0;
                    int maxHeight = 0;
                    foreach (object obj in wrp.Children) {
                        if (obj is FrameworkElement) {
                            int width = (int)(((FrameworkElement)obj).ActualWidth+padding.Left+padding.Left);
                            int height = (int)(((FrameworkElement)obj).ActualHeight+padding.Top+padding.Top);
                            maxWidth = maxWidth < width ? width : maxWidth;
                            maxHeight = maxHeight < height ? height : maxHeight;
                        }
                    }
                    //Width = (elementsPerLine * maxWidth) + padding.Left + (40);
                    Width = Double.NaN;
                    Margin = padding;
                    HorizontalAlignment = HorizontalAlignment.Stretch;

                    int size = images.Length;
                    int maxX = elementsPerLine;
                    int maxY = (size - (size % maxX)) / maxX;
                    maxY += size % maxX == 0 ? 0 : 1;
                    for (int i = 0; i < size; i++) {
                        UIElement element = wrp.Children[i];
                        int x = i % maxX;
                        int y = (i - x) / maxX;
                        if (element is FrameworkElement) {
                            FrameworkElement elem = element as FrameworkElement;
                            elem.Width = maxWidth;
                            elem.Height = maxHeight;
                            elem.Margin = new Thickness((x * maxWidth+padding.Left), (y * maxHeight)+padding.Top, 0, 0);
                            elem.HorizontalAlignment = HorizontalAlignment.Left;
                            elem.VerticalAlignment = VerticalAlignment.Top;
                        }
                    }
                }
            };
        }


    }
}
