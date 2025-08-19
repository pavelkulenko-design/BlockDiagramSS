using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using System.Linq;
using System.Windows;

namespace BlockDiagramSS
{
    public partial class ViewPickerWindow : Window
    {
        // TODO
        public ElementId SelectedViewId { get; private set; }
        public double StepMm { get; private set; }
        public double VerticalOffsetMm { get; private set; }
        public double LineLengthMm { get; private set; }
        public string CircuitFilter { get; private set; }

        public ViewPickerWindow(Document doc)
        {
            InitializeComponent();

            // Это бизнес логика плагина, надо вынести в слой Model
            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewDrafting))
                .Cast<ViewDrafting>()
                .OrderBy(v => v.Name)
                .ToList();

            ViewSelector.ItemsSource = views;
            ViewSelector.DisplayMemberPath = "Name";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var view = ViewSelector.SelectedItem as ViewDrafting;
            if (view != null)
                SelectedViewId = view.Id;

            double step;
            if (double.TryParse(StepBox.Text, out step))
                StepMm = step;

            double offset;
            if (double.TryParse(OffsetBox.Text, out offset))
                VerticalOffsetMm = offset;

            double lineLength;
            if (double.TryParse(LineLengthBox.Text, out lineLength))
                LineLengthMm = lineLength;

            CircuitFilter = CircuitFilterBox.Text;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
