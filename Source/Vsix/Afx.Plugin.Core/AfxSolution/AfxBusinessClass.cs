using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Vsix.Utilities;

namespace Afx.Plugin.AfxSolution
{
  public class AfxBusinessClass : AfxProjectItem
  {
    public const string AfxObject = "Afx.AfxObject";

    #region Contructors

    public AfxBusinessClass(AfxProject project, FileCodeModel fileCodeModel, CodeClass codeClass) 
      : base(project, fileCodeModel)
    {
      CodeClass = codeClass;
    }

    #endregion

    #region AfxBusinessClass BaseClass

    AfxBusinessClass mBaseClass;
    public AfxBusinessClass BaseClass
    {
      get { return mBaseClass; }
      set { SetProperty<AfxBusinessClass>(ref mBaseClass, value); }
    }

    #endregion

    #region ObservableCollection<AfxBusinessClass> DerivedClasses

    ObservableCollection<AfxBusinessClass> mDerivedClasses = new ObservableCollection<AfxBusinessClass>();
    public ObservableCollection<AfxBusinessClass> DerivedClasses
    {
      get { return mDerivedClasses; }
    }

    #endregion

    public bool Refresh()
    {
      bool success = false;
      foreach (CodeElement ce in CodeClass.Bases)
      {
        CodeClass cc = ce as CodeClass;
        if (cc == null) continue;

        BaseClass = AfxBusinessClass.GetBusinessClass(cc.FullName);
        if (cc.FullName.StartsWith(AfxObject) || BaseClass != null)
        {
          if (BaseClass != null && !BaseClass.DerivedClasses.Contains(this)) BaseClass.DerivedClasses.Add(this);
          success = true;
        }
      }

      if (!success)
      {
        Remove();
        return false;
      }

      #region Load / Create Config

      string configPath = null;
      foreach (ProjectItem pi in ProjectItem.ProjectItems)
      {
        if (pi.Kind != ProjectItemKinds.PhysicalFile) continue;
        string fileName = pi.FileNames[0];
        if (fileName.Contains(".config")) configPath = pi.FileNames[0];
      }

      string s = ProjectItem.FileNames[0];
      if (configPath == null)
      {
        configPath = s.Replace(".cs", ".config");
        File.WriteAllText(configPath, string.Format(@"<AfxBusinessClass Guid=""{0}"" />", Guid.NewGuid()));
        ProjectItem.ProjectItems.AddFromFile(configPath);
      }

      Config = new XmlDocument();
      Config.Load(configPath);

      #endregion

      if (!Project.BusinessClasses.Contains(this)) Project.BusinessClasses.Add(this);
      if (!mBusinessClassByNameDictionary.ContainsKey(FullName)) mBusinessClassByNameDictionary.Add(FullName, this);
      if (!mBusinessClassByProjectItemDictionary.ContainsKey(ProjectItem)) mBusinessClassByProjectItemDictionary.Add(ProjectItem, this);
      if (!mBusinessClassByGuidDictionary.ContainsKey(Guid)) mBusinessClassByGuidDictionary.Add(Guid, this);

      ReBind();

      foreach (var dc in DerivedClasses.ToArray())
      {
        dc.Refresh();
      }

      return true;
    }

    public void ReBind()
    {
      OnPropertyChanged(nameof(FullName));
    }

    public void Remove()
    {
      if (BaseClass != null) if (BaseClass.DerivedClasses.Contains(this)) BaseClass.DerivedClasses.Remove(this);
      foreach (var bc in DerivedClasses.ToArray())
      {
        bc.Remove();
      }

      if (Project.BusinessClasses.Contains(this)) Project.BusinessClasses.Remove(this);
      if (mBusinessClassByNameDictionary.ContainsKey(FullName)) mBusinessClassByNameDictionary.Remove(FullName);
      if (mBusinessClassByProjectItemDictionary.ContainsKey(ProjectItem)) mBusinessClassByProjectItemDictionary.Remove(ProjectItem);
      if (mBusinessClassByGuidDictionary.ContainsKey(Guid)) mBusinessClassByGuidDictionary.Remove(Guid);

      AfxSolution.EnqueueUnprocessed(Project, FileCodeModel);
    }

    #region Static

    #region Cache

    static Dictionary<Guid, AfxBusinessClass> mBusinessClassByGuidDictionary = new Dictionary<Guid, AfxBusinessClass>();
    public static AfxBusinessClass GetBusinessClass(Guid guid)
    {
      return mBusinessClassByGuidDictionary.ContainsKey(guid) ? mBusinessClassByGuidDictionary[guid] : null;
    }

    static Dictionary<string, AfxBusinessClass> mBusinessClassByNameDictionary = new Dictionary<string, AfxBusinessClass>();
    public static AfxBusinessClass GetBusinessClass(string fullName)
    {
      return mBusinessClassByNameDictionary.ContainsKey(fullName) ? mBusinessClassByNameDictionary[fullName] : null;
    }

    static Dictionary<ProjectItem, AfxBusinessClass> mBusinessClassByProjectItemDictionary = new Dictionary<ProjectItem, AfxBusinessClass>();
    public static AfxBusinessClass GetBusinessClass(ProjectItem projectItem)
    {
      return mBusinessClassByProjectItemDictionary.ContainsKey(projectItem) ? mBusinessClassByProjectItemDictionary[projectItem] : null;
    }

    public static void ClearCache()
    {
      mBusinessClassByNameDictionary.Clear();
      mBusinessClassByGuidDictionary.Clear();
      mBusinessClassByProjectItemDictionary.Clear();
    }

    #endregion

    #endregion
  }
}
