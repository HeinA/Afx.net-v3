using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.AfxSolution
{
  public class AfxSolution
  {
    private AfxSolution()
    {
    }

    static AfxSolution()
    {
      Instance = new AfxSolution();
    }

    public static AfxSolution Instance { get; private set; }

    #region Class Libraries

    ObservableCollection<AfxProjectClassLibrary> mAfxClassLibraryProjects = new ObservableCollection<AfxProjectClassLibrary>();
    ReadOnlyObservableCollection<AfxProjectClassLibrary> mReadOnlyAfxClassLibraryProjects;
    public ReadOnlyObservableCollection<AfxProjectClassLibrary> AfxClassLibraryProjects
    {
      get { return mReadOnlyAfxClassLibraryProjects ?? (mReadOnlyAfxClassLibraryProjects = new ReadOnlyObservableCollection<AfxProjectClassLibrary>(mAfxClassLibraryProjects)); }
    }

    public void AddAfxClassLibraryProject(AfxProjectClassLibrary project)
    {
      if (!mAfxClassLibraryProjects.Contains(project))
      {
        mAfxClassLibraryProjects.Add(project);
        OnAfxProjectAdded(project);
      }
    }

    #endregion

    #region Data Libraries

    ObservableCollection<AfxProjectDataLibrary> mAfxDataLibraryProjects = new ObservableCollection<AfxProjectDataLibrary>();
    ReadOnlyObservableCollection<AfxProjectDataLibrary> mReadOnlyAfxDataLibraryProjects;
    public ReadOnlyObservableCollection<AfxProjectDataLibrary> AfxDataLibraryProjects
    {
      get { return mReadOnlyAfxDataLibraryProjects ?? (mReadOnlyAfxDataLibraryProjects = new ReadOnlyObservableCollection<AfxProjectDataLibrary>(mAfxDataLibraryProjects)); }
    }

    public void AddAfxDataLibraryProject(AfxProjectDataLibrary project)
    {
      if (!mAfxDataLibraryProjects.Contains(project))
      {
        mAfxDataLibraryProjects.Add(project);
        OnAfxProjectAdded(project);
      }
    }

    #endregion


    #region RemoveAfxProject()

    public void RemoveAfxProject(AfxProject project)
    {
      var afxCLProject = project as AfxProjectClassLibrary;
      if (afxCLProject != null && mAfxClassLibraryProjects.Contains(project))
      {
        mAfxClassLibraryProjects.Remove(afxCLProject);
        OnAfxProjectRemoved(project);
      }

      var afxDLProject = project as AfxProjectDataLibrary;
      if (afxDLProject != null && mAfxDataLibraryProjects.Contains(project))
      {
        mAfxDataLibraryProjects.Remove(afxDLProject);
        OnAfxProjectRemoved(project);
      }
    }

    #endregion

    #region AfxProjectAdded

    public event EventHandler<AfxProjectEventArgs> AfxProjectAdded;
    protected virtual void OnAfxProjectAdded(AfxProject project)
    {
      AfxProjectAdded?.Invoke(this, new AfxProjectEventArgs(project));
    }

    #endregion

    #region AfxProjectRemoved

    public event EventHandler<AfxProjectEventArgs> AfxProjectRemoved;
    protected virtual void OnAfxProjectRemoved(AfxProject project)
    {
      AfxProjectRemoved?.Invoke(this, new AfxProjectEventArgs(project));
    }

    #endregion

    #region PreProcessing

    public static void EnqueueUnprocessed(AfxProject project, FileCodeModel fileCodeModel)
    {
      foreach (CodeElement ce in fileCodeModel.CodeElements)
      {
        if (ce.Kind == vsCMElement.vsCMElementNamespace)
        {
          var cn = ce as CodeNamespace;
          foreach (CodeElement ce1 in cn.Members)
          {
            if (ce1.Kind == vsCMElement.vsCMElementClass)
            {
              CodeClass codeClass = ce1 as CodeClass;
              var pi = new AfxUnprocessedProjectItem(project, fileCodeModel, codeClass);
              if (!mUnprocessedQueue.Contains(pi)) mUnprocessedQueue.Enqueue(pi);
            }
          }
        }
      }
    }

    static Queue<AfxUnprocessedProjectItem> mUnprocessedQueue = new Queue<AfxUnprocessedProjectItem>();
    public static void ProcessQueue()
    {
      int retries = 0;
      while (mUnprocessedQueue.Count > retries)
      {
        AfxUnprocessedProjectItem c = mUnprocessedQueue.Dequeue();
        if (c.Process())
        {
          retries = 0;
        }
        else
        {
          if (!mUnprocessedQueue.Contains(c)) mUnprocessedQueue.Enqueue(c);
          retries++;
        }
      }
    }

    #endregion

    public void Close()
    {
      mAfxClassLibraryProjects.Clear();
      mAfxDataLibraryProjects.Clear();
      mUnprocessedQueue.Clear();
      AfxBusinessClass.ClearCache();

      if (AfxProjectAdded != null)
      {
        foreach (Delegate d in AfxProjectAdded.GetInvocationList())
        {
          AfxProjectAdded -= (EventHandler<AfxProjectEventArgs>)d;
        }
      }

      if (AfxProjectRemoved != null)
      {
        foreach (Delegate d in AfxProjectRemoved.GetInvocationList())
        {
          AfxProjectRemoved -= (EventHandler<AfxProjectEventArgs>)d;
        }
      }
    }
  }
}
