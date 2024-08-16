using OxyPlot;
using OxyPlot.Annotations;

namespace CurveAnalyzer.Tools
{
    public class CustomTextAnnotation : Annotation
    {
        public string Text { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public override void Render(IRenderContext rc)
        {
            base.Render(rc);
            double pX = PlotModel.PlotArea.Left + X;
            double pY = PlotModel.PlotArea.Top + Y;
            rc.DrawMultilineText(new ScreenPoint(pX, pY), Text, TextColor, Font, FontSize, FontWeight);
        }
    }
}
