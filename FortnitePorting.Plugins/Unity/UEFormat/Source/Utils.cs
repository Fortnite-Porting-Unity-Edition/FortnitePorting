namespace Editor.UEFormat.Source
{
    public class Utils
    {
        public static float[,] FlattenedToFloatMatrix(float[] flattened, int rows, int cols, float scale = 1.0f)
        {
            float[,] matrix = new float[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = flattened[i * cols + j] * scale;
                }
            }
            return matrix;
        }

        public static int[,] FlattenedToIntMatrix(int[] flattened, int rows, int cols)
        {
            int[,] matrix = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = flattened[i * cols + j];
                }
            }
            return matrix;
        }
        
        public static float[,] ExtractSubMatrix(float[,] matrix, int colStart, int colEnd, int colsToCopy)
        {
            int rows = matrix.GetLength(0);
            float[,] result = new float[rows, colsToCopy];
            for (int i = 0; i < rows; i++)
            {
                for (int j = colStart; j <= colEnd; j++)
                {
                    result[i, j - colStart] = matrix[i, j];
                }
            }
            return result;
        }
    }
}