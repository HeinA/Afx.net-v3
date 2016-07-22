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
  public class AfxObject : AfxType
  {
    public AfxObject(AfxProject afxProject, FileCodeModel fileCodeModel, CodeClass codeClass)
      : base(afxProject, fileCodeModel, codeClass)
    {
    }

    public AfxObject(AfxAssembly assembly, Type type)
      : base(assembly, type)
    {
    }
    
    public bool IsPersistent
    {
      get { return CodeClass.Attributes.OfType<CodeAttribute>().Any(cc => cc.FullName.StartsWith(ApplicationStructure.AfxPersistentAttribute)); }
      set
      {
        var ca = CodeClass.Attributes.OfType<CodeAttribute>().FirstOrDefault(cc => cc.FullName.StartsWith(ApplicationStructure.AfxPersistentAttribute));
        if (ca == null)
        {
          if (!IsPersistent)
          {
            CodeClass.AddAttribute(ApplicationStructure.AfxPersistentAttribute, null);
            Project.ProcessClass(CodeClass, FileCodeModel);
          }
        }
        else
        {
          ca.Delete();
          RemoveDataRepositories();
        }

        Reformat((CodeElement)CodeClass);
      }
    }

    protected internal override void OnRemove()
    {
      RemoveDataRepositories();
      base.OnRemove();
    }

    void RemoveDataRepositories()
    {
      foreach (var drl in ApplicationStructure.Instance.Projects.OfType<AfxProjectDataLibrary>())
      {
        foreach (var dr in drl.AfxTypes.OfType<AfxObjectRepository>().Where(r => r.TargetType.Equals(this)).ToArray())
        {
          if (dr.FileCodeModel.Parent.IsOpen) dr.FileCodeModel.Parent.Save();
          dr.FileCodeModel.Parent.Remove();
          drl.AfxTypes.Remove(dr);
        }

        foreach (AfxFolder folder in drl.Folders)
        {
          folder.OnRefresh();
        }
      }
    }
  }
}
