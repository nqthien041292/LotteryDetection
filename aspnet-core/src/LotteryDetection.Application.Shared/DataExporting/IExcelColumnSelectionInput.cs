using System.Collections.Generic;

namespace LotteryDetection.DataExporting;

public interface IExcelColumnSelectionInput
{
    List<string> SelectedColumns { get; set; }
}