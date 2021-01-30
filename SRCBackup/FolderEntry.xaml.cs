using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace SRCBackup {
    public partial class FolderEntry {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(FolderEntry), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(FolderEntry), new PropertyMetadata(null));

        public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs args);
        public ValueChangedEventHandler ValueChanged;

        public string Text {
            get => GetValue(TextProperty) as string;
            set => SetValue(TextProperty, value);
        }

        public string Description {
            get => GetValue(DescriptionProperty) as string;
            set => SetValue(DescriptionProperty, value);
        }

        public FolderEntry() {
            InitializeComponent();
        }

        private void BrowseFolder(object sender, RoutedEventArgs e) {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog()) {
                dlg.Description = Description;
                dlg.SelectedPath = Text;
                dlg.ShowNewFolderButton = true;
                var result = dlg.ShowDialog();
                if (result == DialogResult.OK) {
                    var tmp = Text;
                    Text = dlg.SelectedPath;
                    var be = GetBindingExpression(TextProperty);
                    be?.UpdateSource();
                    ValueChanged?.Invoke(this, new ValueChangedEventArgs(tmp, Text));
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(null, Text));
        }
    }

    public class ValueChangedEventArgs : EventArgs {
        public string OldValue;
        public string NewValue;

        public ValueChangedEventArgs(string oldValue, string newValue) {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
