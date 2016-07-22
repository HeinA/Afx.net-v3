using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Afx
{
  [DataContract(Namespace = Constants.WcfNamespace, IsReference = true)]
  [AfxBaseType]
  public abstract class AssociativeObject : AfxObject, IAssociativeObject
  {
    #region Contructors

    internal AssociativeObject()
    {
    }

    #endregion

    #region IAfxObject Reference

    public const string ReferenceProperty = "Reference";
    IAfxObject mReference;
    internal IAfxObject Reference
    {
      get { return mReference; }
      set { mReference = ValidateReference(value); }
    }

    protected virtual IAfxObject ValidateReference(IAfxObject reference)
    {
      if (reference == null) return null;
      throw new InvalidOperationException(Properties.Resources.NoReferenceType);
    }

    IAfxObject IAssociativeObject.Reference
    {
      get { return this.Reference; }
      set { this.Reference = value; }
    }

    #endregion
  }

  [DataContract(Namespace = Constants.WcfNamespace, IsReference = true)]
  [AfxBaseType]
  public abstract class AssociativeObject<TOwner, TReference> : AssociativeObject
    where TOwner : class, IAfxObject
    where TReference : class, IAfxObject
  {
    #region Contructors

    protected AssociativeObject()
    {
    }

    #endregion

    #region TOwner Owner

    [Persistent]
    public new TOwner Owner
    {
      get { return (TOwner)base.Owner; }
      protected set { base.Owner = value; }
    }

    protected override IAfxObject ValidateOwner(IAfxObject owner)
    {
      if (!(owner is TOwner)) throw new InvalidCastException(Properties.Resources.InvalidOwnerType);
      return owner;
    }

    #endregion

    #region TReference Reference

    [Persistent]
    public new TReference Reference
    {
      get { return (TReference)base.Reference; }
      set { base.Reference = value; }
    }

    protected override IAfxObject ValidateReference(IAfxObject reference)
    {
      if (!(reference is TReference)) throw new InvalidCastException(Properties.Resources.InvalidReferenceType);
      return reference;
    }

    #endregion
  }
}
