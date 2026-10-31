using System;
using System.Windows.Forms;

namespace PortfolioSamples.Demo
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new MainForm()); }
            catch (Exception)
            {
                // 최종 UI 경계에서만 예외를 묶어 안내합니다. 예외 원문의 경로·계정정보는 화면에 표시하지 않습니다.
                MessageBox.Show("샘플을 시작하지 못했습니다. 작업 폴더의 권한과 설정을 확인하세요.", "공개 코드 샘플", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
