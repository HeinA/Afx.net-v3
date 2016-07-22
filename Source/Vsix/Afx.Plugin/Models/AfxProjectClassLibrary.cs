using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public sealed class AfxProjectClassLibrary : AfxProject
  {
    internal AfxProjectClassLibrary(Project project)
      : base(project)
    {
    }

    public override IEnumerable<AfxFolder> Folders
    {
      get
      {
        yield return AfxObjectClassFolder;
      }
    }
  }
}