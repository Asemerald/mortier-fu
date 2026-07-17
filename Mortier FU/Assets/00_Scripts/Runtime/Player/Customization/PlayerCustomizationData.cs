namespace MortierFu
{
    public sealed class PlayerCustomizationData
    {
        public int SkinIndex { get; private set; } = 0;
        public int FaceColumn { get; private set; } = 1;
        public int FaceRow { get; private set; } = 1;

        public void SetCustomization(int skinIndex, int faceColumn, int faceRow)
        {
            SkinIndex = skinIndex;
            FaceColumn = faceColumn;
            FaceRow = faceRow;
        }
    }
}