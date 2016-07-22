using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.AfxSolution
{
  public class AfxUnprocessedProjectItem
  {
    public AfxUnprocessedProjectItem(AfxProject project, FileCodeModel fileCodeModel, CodeClass codeClass)
    {
      Project = project;
      FileCodeModel = fileCodeModel;
      ProjectItem = fileCodeModel.Parent;
      CodeClass = codeClass;
    }

    public AfxProject Project { get; private set; }
    public FileCodeModel FileCodeModel { get; private set; }
    public ProjectItem ProjectItem { get; private set; }
    public CodeClass CodeClass { get; private set; }

    public bool Process()
    {
      AfxBusinessClass c = AfxBusinessClass.GetBusinessClass(ProjectItem);
      if (c != null && c.Refresh()) return true;

      AfxObjectRepository or = AfxObjectRepository.GetObjectRepository(ProjectItem);
      if (or != null && or.Refresh()) return true;

      foreach (CodeElement ce in CodeClass.Bases)
      {
        CodeClass cc = ce as CodeClass;
        if (cc == null) continue;

        var baseClass = AfxBusinessClass.GetBusinessClass(cc.FullName);
        if (cc.FullName.StartsWith(AfxBusinessClass.AfxObject) || baseClass != null)
        {
          c = new AfxBusinessClass(Project, FileCodeModel, CodeClass);
          if (c.Refresh()) return true;
        }

        AfxProjectDataLibrary dl = Project as AfxProjectDataLibrary;
        if (dl != null)
        {
          if (cc.FullName.StartsWith(AfxObjectRepository.AfxObjectRepositoryName))
          {
            or = new AfxObjectRepository(dl, FileCodeModel, CodeClass);
            if (or.Refresh()) return true;
          }
        }
      }

      return false;
    }

    public override int GetHashCode()
    {
      return CodeClass.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      AfxUnprocessedProjectItem pi = obj as AfxUnprocessedProjectItem;
      if (pi == null) return false;
      return CodeClass.Equals(pi.CodeClass);
    }
  }
}
