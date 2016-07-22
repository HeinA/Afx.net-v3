using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Vsix.Utilities;

namespace Afx.Plugin.AfxSolution
{
  public class AfxObjectRepository : AfxProjectItem
  {
    public const string AfxObjectRepositoryName = "Afx.Data.ObjectRepository";

    #region Contructors

    public AfxObjectRepository(AfxProjectDataLibrary project, FileCodeModel fileCodeModel, CodeClass codeClass)
      : base(project, fileCodeModel)
    {
      CodeClass = codeClass;
    }

    #endregion

    public new AfxProjectDataLibrary Project
    {
      get { return (AfxProjectDataLibrary)base.Project; }
    }

    #region AfxBusinessClass TargetClass

    public AfxBusinessClass TargetClass
    {
      get
      {
        CodeClass ci = CodeClass.Bases.OfType<CodeClass>().FirstOrDefault(ci1 => ci1.FullName.StartsWith(AfxObjectRepository.AfxObjectRepositoryName));
        string targetClassName = ci.FullName.Split('<', '>')[1];
        return AfxBusinessClass.GetBusinessClass(targetClassName);
      }
    }

    #endregion

    public bool Refresh()
    {
      bool success = false;
      foreach (CodeElement ce in CodeClass.Bases)
      {
        CodeClass cc = ce as CodeClass;
        if (cc == null) continue;

        if (cc.FullName.StartsWith(AfxObjectRepositoryName))
        {
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
        File.WriteAllText(configPath, string.Format(@"<AfxObjectRepositoryClass Guid=""{0}"" />", Guid.NewGuid()));
        ProjectItem.ProjectItems.AddFromFile(configPath);
      }

      Config = new XmlDocument();
      Config.Load(configPath);

      #endregion

      if (!Project.Repositories.Contains(this)) Project.Repositories.Add(this);
      if (!ObjectRepositories.Contains(this)) ObjectRepositories.Add(this);
      if (!mObjectRepositoryByNameDictionary.ContainsKey(FullName)) mObjectRepositoryByNameDictionary.Add(FullName, this);
      if (!mObjectRepositoryByProjectItemDictionary.ContainsKey(ProjectItem)) mObjectRepositoryByProjectItemDictionary.Add(ProjectItem, this);
      if (!mObjectRepositoryByGuidDictionary.ContainsKey(Guid)) mObjectRepositoryByGuidDictionary.Add(Guid, this);

      ReBind();

      return true;
    }

    public void ReBind()
    {
      OnPropertyChanged(nameof(FullName));
      OnPropertyChanged(nameof(TargetClass));
    }

    public void Remove()
    {
      if (Project.Repositories.Contains(this)) Project.Repositories.Remove(this);
      if (ObjectRepositories.Contains(this)) ObjectRepositories.Remove(this);
      if (mObjectRepositoryByNameDictionary.ContainsKey(FullName)) mObjectRepositoryByNameDictionary.Remove(FullName);
      if (mObjectRepositoryByProjectItemDictionary.ContainsKey(ProjectItem)) mObjectRepositoryByProjectItemDictionary.Remove(ProjectItem);
      if (mObjectRepositoryByGuidDictionary.ContainsKey(Guid)) mObjectRepositoryByGuidDictionary.Remove(Guid);

      AfxSolution.EnqueueUnprocessed(Project, FileCodeModel);
    }

    #region Static

    #region Cache

    static Collection<AfxObjectRepository> mObjectRepositories = new Collection<AfxObjectRepository>();
    public static Collection<AfxObjectRepository> ObjectRepositories { get { return mObjectRepositories; } }

    static Dictionary<Guid, AfxObjectRepository> mObjectRepositoryByGuidDictionary = new Dictionary<Guid, AfxObjectRepository>();
    public static AfxObjectRepository GetObjectRepository(Guid guid)
    {
      return mObjectRepositoryByGuidDictionary.ContainsKey(guid) ? mObjectRepositoryByGuidDictionary[guid] : null;
    }

    static Dictionary<string, AfxObjectRepository> mObjectRepositoryByNameDictionary = new Dictionary<string, AfxObjectRepository>();
    public static AfxObjectRepository GetObjectRepository(string fullName)
    {
      return mObjectRepositoryByNameDictionary.ContainsKey(fullName) ? mObjectRepositoryByNameDictionary[fullName] : null;
    }

    static Dictionary<ProjectItem, AfxObjectRepository> mObjectRepositoryByProjectItemDictionary = new Dictionary<ProjectItem, AfxObjectRepository>();
    public static AfxObjectRepository GetObjectRepository(ProjectItem projectItem)
    {
      return mObjectRepositoryByProjectItemDictionary.ContainsKey(projectItem) ? mObjectRepositoryByProjectItemDictionary[projectItem] : null;
    }

    public static void ClearCache()
    {
      mObjectRepositoryByNameDictionary.Clear();
      mObjectRepositoryByGuidDictionary.Clear();
      mObjectRepositoryByProjectItemDictionary.Clear();
    }

    #endregion

    #endregion
  }
}
