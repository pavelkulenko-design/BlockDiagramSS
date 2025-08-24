using Autodesk.Revit.DB;
using System.Linq;
using System.Windows;

namespace BlockDiagramSS
{
    // Ветка для работы с багами
    // После устранения багов объеденить с веткой master
    public partial class ViewPickerWindow : Window
    {
        public ElementId SelectedViewId { get; private set; }
        public double StepMm { get; private set; }
        public double VerticalOffsetMm { get; private set; }
        public double LineLengthMm { get; private set; }
        public string CircuitFilter { get; private set; }

        /// <summary>
        /// Конструктор класса который создает окно
        /// </summary>
        /// <param name="doc"></param>
        public ViewPickerWindow(Document doc)
        {
            InitializeComponent();

            // Вот эту часть кода в идеале вынести из конструктора окна так как она относится к бизнес-логике плагина. 
            // Для полного понимания изучить архитектурный паттерн MVVM
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
