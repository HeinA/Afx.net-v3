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
  public abstract class AfxType
  {
    public AfxProject Project { get; private set; }
    public FileCodeModel FileCodeModel { get; private set; }
    public CodeClass CodeClass { get; private set; }
    
    public AfxAssembly Assembly { get; private set; }
    public Type Type { get; private set; }

    protected AfxType(AfxProject afxProject, FileCodeModel fileCodeModel, CodeClass codeClass)
    {
      Project = afxProject;
      FileCodeModel = fileCodeModel;
      CodeClass = codeClass;
    }

    protected AfxType(AfxAssembly assembly, Type type)
    {
      Assembly = assembly;
      Type = type;
    }

    public bool IsProjectItem
    {
      get { return Project != null; }
    }

    public bool IsPublic
    {
      get
      {
        if (IsProjectItem) return CodeClass.Access == vsCMAccess.vsCMAccessPublic;
        return Type.IsPublic;
      }
    }

    public string FullName
    {
      get { return CodeClass != null ? CodeClass.FullName : Type.FullName; }
    }

    public string Name
    {
      get
      {
        string fullName = FullName;
        if (!fullName.Contains('.')) return fullName;
        int index = fullName.LastIndexOf('.') + 1;
        return fullName.Substring(index);
      }
    }

    public void Open()
    {
      if (FileCodeModel == null) return;
      Window w = FileCodeModel.Parent.Open();
      w.Visible = true;
    }

    public ICommand ItemActivated
    {
      get { return new DelegateCommand<AfxType>(OnItemActivated); }
    }

    private void OnItemActivated(AfxType item)
    {
      Open();
    }

    protected void Reformat(CodeElement ce)
    {
      var objMovePt = ce.EndPoint.CreateEditPoint();
      var objEditPt = ce.StartPoint.CreateEditPoint();
      objEditPt.StartOfDocument();
      objMovePt.EndOfDocument();
      objMovePt.SmartFormat(objEditPt);
    }

    protected internal virtual void OnRemove()
    {
    }
  }
}
