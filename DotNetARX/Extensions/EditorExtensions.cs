namespace DotNetARX.Extensions
{
    /// <summary>
    /// Editor 扩展方法
    /// 提供便捷的编辑器操作方法
    /// </summary>
    public static class EditorExtensions
    {
        /// <summary>
        /// 根据多边形选择实体
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <param name="polygon">多边形顶点</param>
        /// <param name="crossingMode">是否使用交叉模式</param>
        /// <returns>选择结果</returns>
        public static PromptSelectionResult SelectPolygon(this Editor editor, Point3dCollection polygon, bool crossingMode = false)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            if (polygon == null || polygon.Count < 3)
                throw new ArgumentException("多边形至少需要3个点", nameof(polygon));

            try
            {
                return editor.SelectCrossingPolygon(polygon);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(EditorExtensions))?.Error($"多边形选择失败: {ex.Message}", ex);
                return new PromptSelectionResult(PromptStatus.Error);
            }
        }

        /// <summary>
        /// 根据多边形选择实体（使用Point3d数组）
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <param name="polygonPoints">多边形顶点数组</param>
        /// <param name="crossingMode">是否使用交叉模式</param>
        /// <returns>选择结果</returns>
        public static PromptSelectionResult SelectPolygon(this Editor editor, Point3d[] polygonPoints, bool crossingMode = false)
        {
            if (polygonPoints == null || polygonPoints.Length < 3)
                throw new ArgumentException("多边形至少需要3个点", nameof(polygonPoints));

            var polygon = new Point3dCollection();
            foreach (var point in polygonPoints)
            {
                polygon.Add(point);
            }

            return SelectPolygon(editor, polygon, crossingMode);
        }
    }
}