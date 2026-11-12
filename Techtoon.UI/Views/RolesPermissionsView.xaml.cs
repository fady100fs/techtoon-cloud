using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    /// <summary>
    /// صفحة «الأدوار والصلاحيات»:
    /// مصفوفة صلاحيات لكل دور، وتوزيع الأدوار على المستخدمين، واستثناءات لكل مستخدم.
    /// </summary>
    public partial class RolesPermissionsView : UserControl
    {
        public RolesPermissionsView()
        {
            InitializeComponent();
            DataContext = new RolesPermissionsViewModel();
        }
    }
}
