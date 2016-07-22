using Afx.Plugin.Commands;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Afx.Plugin.Models
{
  public class AfxObjectClass
  {
    public AfxProject AfxProject { get; private set; }
    public FileCodeModel FileCodeModel { get; private set; }
    public CodeClass CodeClass { get; private set; }

    public AfxObjectClass(AfxProject afxProject, FileCodeModel fileCodeModel, CodeClass codeClass)
    {
      AfxProject = afxProject;
      FileCodeModel = fileCodeModel;
      CodeClass = codeClass;
    }

    public string Name
    {
      get { return CodeClass.Name; }
    }

    public void Open()
    {
      Window w = FileCodeModel.Parent.Open();
      w.Visible = true;
    }

    public ICommand ItemActivated
    {
      get { return new DelegateCommand<AfxObjectClass>(OnItemActivated); }
    }

    private void OnItemActivated(AfxObjectClass item)
    {
      Open();
    }
  }
}
