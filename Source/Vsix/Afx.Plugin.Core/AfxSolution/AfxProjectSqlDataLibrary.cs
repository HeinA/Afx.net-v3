using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.AfxSolution
{
  public class AfxProjectSqlDataLibrary : AfxProjectDataLibrary
  {
    public AfxProjectSqlDataLibrary(Project project)
      : base(project)
    {
    }
  }
}
