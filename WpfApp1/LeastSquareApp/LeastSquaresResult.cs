namespace WpfApp1.LeastSquareApp
{
    public class LeastSquaresResult
    {
        public int Degree { get; set; }
        public double[] Coefficients { get; set; } = new double[0];
        public double SSE { get; set; }
        public double RMSE { get; set; }
        public double R2 { get; set; }
    }
}
