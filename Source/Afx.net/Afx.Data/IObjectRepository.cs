//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Afx.Data
//{
//  public interface IObjectRepository
//  {
//    Type TargetType { get; }

//    bool IsNew(Guid id);

//    IAfxObject LoadObjectCore(LoadContext context);
//    void LoadObjectCore(IAfxObject target, LoadContext context);

//    //void FillObject(IAfxObject target, LoadContext context);

//    IAfxObject[] LoadObjectsCore(LoadContext context);

//    void SaveObjectCore(IAfxObject target, SaveContext context);

//    void DeleteObjectsCore(DeleteContext context);
//  }
//}
