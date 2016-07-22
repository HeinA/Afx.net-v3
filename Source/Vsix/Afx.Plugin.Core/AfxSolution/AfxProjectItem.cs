using Afx.Plugin.Commands;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Afx.Plugin.AfxSolution
{
  public abstract class AfxProjectItem : Model
  {
    protected AfxProjectItem(AfxProject project, FileCodeModel fileCodeModel)
    {
      Project = project;
      FileCodeModel = fileCodeModel;
      ProjectItem = fileCodeModel.Parent;
    }

    public AfxProject Project { get; private set; }
    public FileCodeModel FileCodeModel { get; private set; }
    public ProjectItem ProjectItem { get; private set; }
    public Guid Guid { get { return Guid.Parse(Config.DocumentElement.Attributes["Guid"].Value); } }
    public CodeClass CodeClass { get; protected set; }
    public string Name { get { return CodeClass.Name; } }
    public string FullName { get { return CodeClass.FullName; } }
    public XmlDocument Config { get; protected set; }

    public override bool Equals(object obj)
    {
      AfxProjectItem pi = obj as AfxProjectItem;
      if (pi == null) return false;
      return Guid.Equals(pi.Guid);
    }

    #region DelegateCommand<AfxProjectItem> ItemActivatedCommand

    DelegateCommand<AfxProjectItem> mItemActivatedCommand;
    public DelegateCommand<AfxProjectItem> ItemActivatedCommand
    {
      get { return mItemActivatedCommand ?? (mItemActivatedCommand = new DelegateCommand<AfxProjectItem>(ExecuteItemActivated)); }
    }

    void ExecuteItemActivated(AfxProjectItem args)
    {
      Open();
    }

    public void Open()
    {
      if (ProjectItem == null) return;
      Window w = ProjectItem.Open();
      w.Visible = true;
    }

    protected void Reformat()
    {
      var objMovePt = CodeClass.EndPoint.CreateEditPoint();
      var objEditPt = CodeClass.StartPoint.CreateEditPoint();
      objEditPt.StartOfDocument();
      objMovePt.EndOfDocument();
      objMovePt.SmartFormat(objEditPt);
    }

    #endregion

    public override int GetHashCode()
    {
      return Guid.GetHashCode();
    }

    public override string ToString()
    {
      return FullName;
    }
  }
}
