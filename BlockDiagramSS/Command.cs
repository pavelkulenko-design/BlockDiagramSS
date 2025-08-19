using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using Autodesk.Revit.DB.Electrical;
using System.Collections.Generic;
using System.Linq;

namespace BlockDiagramSS
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Окно выбора параметров
            BlockDiagramWindow window = new BlockDiagramWindow(doc);
            if (window.ShowDialog() != true)
                return Result.Cancelled;

            double stepFt = window.StepMm / 304.8;
            double offsetFt = window.VerticalOffsetMm / 304.8;
            double userLineLengthFt = window.LineLengthMm / 304.8;
            double minLengthFt = doc.Application.ShortCurveTolerance;
            double finalLineLengthFt = Math.Max(userLineLengthFt, minLengthFt);
            string circuitFilter = window.CircuitFilter;

            ViewDrafting draftingView = doc.GetElement(window.SelectedViewId) as ViewDrafting;
            if (draftingView == null)
            {
                TaskDialog.Show("Ошибка", "Не выбран чертежный вид.");
                return Result.Failed;
            }

            // Получаем все цепи
            var circuits = new FilteredElementCollector(doc)
                .OfClass(typeof(ElectricalSystem))
                .Cast<ElectricalSystem>()
                .ToList();

            if (!string.IsNullOrEmpty(circuitFilter))
            {
                circuits = circuits
                    .Where(c => !string.IsNullOrEmpty(c.Name) &&
                                c.Name.IndexOf(circuitFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            if (!circuits.Any())
            {
                TaskDialog.Show("Результат", "Не найдено цепей по фильтру.");
                return Result.Cancelled;
            }

            // Загружаем символ RAM_UGO_Sheme_SS
            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DetailComponents)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name == "RAM_UGO_Sheme_SS");

            if (symbol == null)
            {
                TaskDialog.Show("Ошибка", "Не найдено семейство RAM_UGO_Sheme_SS.");
                return Result.Failed;
            }

            using (Transaction tr = new Transaction(doc, "BlockDiagramSS"))
            {
                tr.Start();

                if (!symbol.IsActive)
                    symbol.Activate();

                XYZ basePoint = new XYZ(0, 0, 0);
                int circuitIndex = 0;

                // 👉 ширина символа 500 мм
                double blockSizeFt = 500.0 / 304.8;
                double halfBlockFt = blockSizeFt / 2.0;

                foreach (var circuit in circuits)
                {
                    var elems = circuit.Elements.Cast<Element>()
                        .OrderBy(e => e.LookupParameter("ADSK_Обозначение")?.AsString())
                        .ToList();

                    XYZ current = basePoint + new XYZ(0, -circuitIndex * offsetFt, 0);

                    for (int i = 0; i < elems.Count; i++)
                    {
                        Element e = elems[i];

                        // Размещаем символ
                        FamilyInstance fi = doc.Create.NewFamilyInstance(current, symbol, draftingView);

                        // Записываем обозначение
                        string mark = e.LookupParameter("ADSK_Обозначение")?.AsString();
                        if (!string.IsNullOrEmpty(mark))
                            fi.LookupParameter("ADSK_Обозначение")?.Set(mark);

                        // Если не последний элемент — рисуем линию от края до края
                        if (i < elems.Count - 1)
                        {
                            XYZ next = current + new XYZ(stepFt, 0, 0);

                            XYZ start = current + new XYZ(halfBlockFt, 0, 0);   // правая грань текущего блока
                            XYZ end = next - new XYZ(halfBlockFt, 0, 0);     // левая грань следующего блока

                            Line line = Line.CreateBound(start, end);
                            if (line.Length > minLengthFt)
                                doc.Create.NewDetailCurve(draftingView, line);

                            // 📏 Манхэттенское расстояние в 3D
                            Element nextElem = elems[i + 1];
                            XYZ p1 = GetElementLocation(e);
                            XYZ p2 = GetElementLocation(nextElem);
                            double manhattan = Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y) + Math.Abs(p1.Z - p2.Z);

                            double manhattanMm = manhattan * 304.8;
                            fi.LookupParameter("ADSK_Размер_Длина")?.Set(manhattanMm);

                            current = next;
                        }
                    }

                    circuitIndex++;
                }

                tr.Commit();
            }

            return Result.Succeeded;
        }

        private XYZ GetElementLocation(Element e)
        {
            LocationPoint lp = e.Location as LocationPoint;
            if (lp != null)
                return lp.Point;

            LocationCurve lc = e.Location as LocationCurve;
            if (lc != null)
                return lc.Curve.GetEndPoint(0);

            return XYZ.Zero;
        }
    }
}
