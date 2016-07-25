using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class ObjectRepositoryBuilder
  {
    public static void DoRepositoryRebuild()
    {
      Guard.ThrowOperationExceptionIfNull(DataScope.CurrentScope, Properties.Resources.NoConnectionScope);
      IObjectRepositoryBuilder builder = Afx.ExtensibilityManager.GetObject<IObjectRepositoryBuilder>(DataScope.CurrentScope);
      builder.BuildRepositories();
    }
  }
}
