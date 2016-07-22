using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Collections
{
  [CollectionDataContract(Namespace = Constants.WcfNamespace, IsReference = true)]
  public class ObjectCollection<TItem> : ObservableCollection<TItem>, IObjectCollection
    where TItem : class, IAfxObject
  {
    #region Constructors

    public ObjectCollection()
    {
    }

    public ObjectCollection(IEnumerable<TItem> collection)
      : base(collection)
    {
    }

    internal ObjectCollection(AfxObject owner, string propertyName)
      : this()
    {
      Owner = owner;
      PropertyName = propertyName;
    }

    #endregion

    #region BusinessObject Owner

    AfxObject mOwner;
    internal AfxObject Owner
    {
      get { return mOwner; }
      private set { mOwner = value; }
    }

    #endregion

    #region string PropertyName

    string mPropertyName;
    internal string PropertyName
    {
      get { return mPropertyName; }
      private set { mPropertyName = value; }
    }

    #endregion

    #region Items

    #region void InsertItem(...)

    protected override void InsertItem(int index, TItem item)
    {
      InsertItemCore(index, item);
      base.InsertItem(index, item);
      OnObjectEdited();
    }

    #endregion

    #region void RemoveItem(...)

    protected override void RemoveItem(int index)
    {
      TItem item = this[index];
      base.RemoveItem(index);
      RemoveItemCore(item);
      OnObjectEdited();
    }

    #endregion

    #region void ClearItems(...)

    protected override void ClearItems()
    {
      foreach (TItem item in this)
      {
        RemoveItemCore(item);
      }
      base.ClearItems();
      OnObjectEdited();
    }

    #endregion

    #region void SetItem(...)

    protected override void SetItem(int index, TItem item)
    {
      TItem oldItem = this[index];
      base.SetItem(index, item);
      RemoveItemCore(oldItem);
      InsertItemCore(index, item);
      OnObjectEdited();
    }

    #endregion

    #region void AddItemCore(...)

    protected virtual void InsertItemCore(int index, TItem item)
    {
      item.Owner = Owner;
      item.PropertyChanged += ItemPropertyChanged;
      mDictionary.Add(item.Id, item);
    }

    #endregion

    #region void RemoveItemCore(...)

    protected virtual void RemoveItemCore(TItem item)
    {
      item.Owner = null;
      item.PropertyChanged -= ItemPropertyChanged;
      mDictionary.Remove(item.Id);
    }

    #endregion

    #region void ItemPropertyChanged(...)

    protected virtual void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      OnObjectEdited();
    }

    #endregion

    #region void OnCollectionChanged(...)

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
      base.OnCollectionChanged(e);
      if (Owner != null) Owner.OnCollectionChanged(this, e);
    }

    #endregion

    #endregion

    #region TItem this[Guid id]

    Dictionary<Guid, TItem> mDictionary = new Dictionary<Guid, TItem>();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers")]
    public TItem this[Guid id]
    {
      get { return mDictionary[id]; }
    }

    #endregion

    #region IEditNotifier

    public event EventHandler ObjectEdited;

    protected virtual void OnObjectEdited()
    {
      if (ObjectEdited != null) ObjectEdited(this, EventArgs.Empty);
    }

    void IEditNotifier.OnObjectEdited()
    {
      OnObjectEdited();
    }

    #endregion

    #region IObjectCollection

    public Type ItemType
    {
      get { return typeof(TItem); }
    }

    void IObjectCollection.Add(IAfxObject item)
    {
      this.Add((TItem)item);
    }

    #endregion
  }
}
