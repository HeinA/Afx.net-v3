using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public interface IObjectRepositoryBuilder
  {
    Assembly BuildRepository(Type type, bool debug, bool inMemory);
    string GetAssemblyName(Type type);
    string GetRepositoryTypeName(Type type);
    string GetRepositoryTypeFullName(Type type);
  }
}
