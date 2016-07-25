using Afx.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Collections
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")
  , CollectionDataContract(Namespace = Constants.WcfNamespace
    , Name = Constants.WcfAssociativeCollectionName
    , ItemName = Constants.WcfAssociativeCollectionItemName
    , KeyName = Constants.WcfAssociativeCollectionReferenceName
    , ValueName = Constants.WcfAssociativeCollectionAssociativeName
    , IsReference = true)]
  public sealed class AssociativeCollection<TItem, TAssociative> : ObjectCollection<TItem>, IAssociativeCollection
    where TItem : class, IAfxObject
    where TAssociative : class, IAssociativeObject, new()
  {
    #region Constructors

    internal AssociativeCollection(AfxObject owner, string propertyName)
      : base(owner, propertyName)
    {
      IsCompositeReference = typeof(TAssociative).GetTypeInfo().GetCustomAttribute<OwnedReferenceAttribute>(true) != null;
      mOrderedDictionary = new OrderedDictionary<TItem, TAssociative>();
      mDictionary = mOrderedDictionary;
    }

    #endregion

    #region bool IsCompositeReference

    bool mIsCompositeReference;
    bool IsCompositeReference
    {
      get { return mIsCompositeReference; }
      set { mIsCompositeReference = value; }
    }

    #endregion

    #region Items

    #region InsertItemCore(...)

    protected override void InsertItemCore(int index, TItem item)
    {
      TAssociative ass = null;
      if (!mDictionary.Contains(item))
      {
        ass = new TAssociative();
        mOrderedDictionary.Insert(index, item, ass);
      }
      else
      {
        ass = (TAssociative)mDictionary[item];
      }

      ass.Reference = item;
      ass.Owner = Owner;
      ass.PropertyChanged -= ItemPropertyChanged;
      ass.PropertyChanged += ItemPropertyChanged;
      if (IsCompositeReference)
      {
        item.PropertyChanged -= ItemPropertyChanged;
        item.PropertyChanged += ItemPropertyChanged;
      }

      RemoveDeletedItem(ass);
    }

    #endregion

    #region RemoveItemCore(...)

    protected override void RemoveItemCore(TItem item)
    {
      if (mDictionary.Contains(item))
      {
        TAssociative ass = (TAssociative)mDictionary[item];
        ass.PropertyChanged -= ItemPropertyChanged;
        mDictionary.Remove(item);
        RemoveDeletedItem(ass);
      }
      if (IsCompositeReference) item.PropertyChanged -= ItemPropertyChanged;

    }

    #endregion

    #endregion

    #region TAssociative this[TItem item]

    public TAssociative this[TItem item]
    {
      get { return (TAssociative)mDictionary[item]; }
    }

    #endregion

    #region IAssociativeCollection

    Type IAssociativeCollection.AssociativeType
    {
      get { return typeof(TAssociative); }
    }

    OrderedDictionary<TItem, TAssociative> mOrderedDictionary = null;
    IDictionary mDictionary = null;

    void IDictionary.Add(object key, object value)
    {
      mDictionary.Add(key, value);
      this.Add((TItem)key);
    }

    void IDictionary.Clear()
    {
      this.Clear();
    }

    bool IDictionary.Contains(object key)
    {
      return mDictionary.Contains(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
      return mDictionary.GetEnumerator();
    }

    bool IDictionary.IsFixedSize
    {
      get { return false; }
    }

    bool IDictionary.IsReadOnly
    {
      get { return mDictionary.IsReadOnly; }
    }

    ICollection IDictionary.Keys
    {
      get { return mDictionary.Keys; }
    }

    void IDictionary.Remove(object key)
    {
      Remove((TItem)key);
    }

    ICollection IDictionary.Values
    {
      get { return mDictionary.Values; }
    }

    object IDictionary.this[object key]
    {
      get
      {
        return mDictionary[key];
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      throw new NotImplementedException();
    }

    int ICollection.Count
    {
      get { return this.Count; }
    }

    bool ICollection.IsSynchronized
    {
      get { return ((ICollection)mDictionary).IsSynchronized; }
    }

    object ICollection.SyncRoot
    {
      get { return ((ICollection)mDictionary).SyncRoot; }
    }

    #endregion
  }
}
