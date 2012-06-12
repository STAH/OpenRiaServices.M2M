namespace RIAServices.M2M.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.Serialization;
    using System.ServiceModel.DomainServices.Server;
    using System.Web.DomainServices.FluentMetadata;

    using RIAServices.M2M;
    using RIAServices.M2M.Utilities;

    public class M2M4RIAExpression<TObject1, TObject2, TLinkTable>
        where TObject1 : class where TObject2 : class where TLinkTable : LinkTable<TObject1, TObject2>
    {
        #region Constants and Fields

        private readonly MetadataContainer metaDataContainer;

        #endregion

        #region Constructors and Destructors

        public M2M4RIAExpression(MetadataContainer metaDataContainer, string m2m1, string m2mview1, string m2mview2, string m2m2)
        {
            this.metaDataContainer = metaDataContainer;

            var linkTableMetaData = metaDataContainer.Entity<TLinkTable>();
            var object1MeataData = metaDataContainer.Entity<TObject1>();
            var object2MeataData = metaDataContainer.Entity<TObject2>();

            object1MeataData.AddMetadata(m2mview1, new IncludeAttribute());
            object1MeataData.AddMetadata(m2mview2, new IncludeAttribute());

            var object1Keys = GetKeys<TObject1>(metaDataContainer);
            var object2Keys = GetKeys<TObject2>(metaDataContainer);

            SetupLinkTableAssociations(x => x.Object1, m2mview1, object1Keys.ToList());
            SetupLinkTableAssociations(x => x.Object2, m2mview2, object2Keys.ToList());

            MarkLinkTableKeysAsDataMembers(linkTableMetaData);

            //linkTableMetaData.AddMetadata(new LinkTableAttribute());

            object1MeataData.AddMetadata(
                m2mview1,
                new LinkTableViewAttribute
                    { LinkTableType = typeof(TLinkTable), M2MPropertyName = m2m1, ElementType = typeof(TObject2) });
            object2MeataData.AddMetadata(
                m2mview2,
                new LinkTableViewAttribute
                    { LinkTableType = typeof(TLinkTable), M2MPropertyName = m2m2, ElementType = typeof(TObject1) });
        }

        #endregion

        #region Methods

        private IEnumerable<string> GetKeys<T>(MetadataContainer container) where T : class
        {
            var typeDescr = container.GetTypeDescriptor(typeof(T));
            var properties = typeDescr.GetProperties().OfType<PropertyDescriptor>();
            var result = properties.Where(p => p.Attributes[typeof(KeyAttribute)] != null).Select(x => x.Name).ToList();
            return result;
        }

        /// <summary>
        /// A linkTable entity contains many properties that can be annotated as KeyAttribute. In this method we mark all of them to be excluded (using the ExceludeAttribute), except for
        /// those of them that are actually allocated as key properties. This way, the generated entity at the client will only have properties that really are KeyAttributes.
        /// </summary>
        /// <param name="metaData"></param>
        private void MarkLinkTableKeysAsDataMembers(MetadataClass<TLinkTable> metaData)
        {
            var linkTableMetaData = metaDataContainer.Entity<TLinkTable>();

            var typeDescr = metaDataContainer.GetTypeDescriptor(typeof(TLinkTable));

            foreach(var propDescr in typeDescr.GetProperties().OfType<PropertyDescriptor>())
            {
                var memberMetadata = metaData.GetMemberMetadata(propDescr.Name);
                if(memberMetadata == null
                   ||
                   (memberMetadata.OfType<KeyAttribute>().Any() == false
                    && memberMetadata.OfType<AssociationAttribute>().Any() == false))
                {
                    linkTableMetaData.AddMetadata(propDescr.Name, new ExcludeAttribute());
                }
            }
        }

        private void SetupLinkTableAssociations<T>(
            Expression<Func<TLinkTable, T>> propertySelector, string viewProperty, IList<string> primaryKeyNames)
            where T : class
        {
            var linkTableMetaData = metaDataContainer.Entity<TLinkTable>();
            var tMetaData = metaDataContainer.Entity<T>();

            var linkTableNavProp = propertySelector.GetProperty();
            var foreignKeyNames = new List<string>();

            linkTableMetaData.AddMetadata(linkTableNavProp.Name, new IncludeAttribute());

            for(var i = 0; i < primaryKeyNames.Count(); i++)
            {
                var key = primaryKeyNames[i];
                var propInfo = typeof(T).GetProperty(key);

                var foreignKeyProp = string.Format(
                    "{0}_{1}_Id{2}", linkTableNavProp.Name, propInfo.PropertyType.Name, i);

                linkTableMetaData.AddMetadata(foreignKeyProp, new KeyAttribute());
                linkTableMetaData.AddMetadata(foreignKeyProp, new DataMemberAttribute());
                linkTableMetaData.AddMetadata(foreignKeyProp, new RoundtripOriginalAttribute());
                foreignKeyNames.Add(foreignKeyProp);
            }
            var primaryKeys = string.Join(",", primaryKeyNames);
            var foreignKeys = string.Join(",", foreignKeyNames);

            var assocName = string.Format("{0}_{1}", typeof(T).Name, viewProperty);
            var assoc1 = new AssociationAttribute(assocName, primaryKeys, foreignKeys);
            var assoc2 = new AssociationAttribute(assocName, foreignKeys, primaryKeys) { IsForeignKey = true };

            tMetaData.AddMetadata(viewProperty, assoc1);
            linkTableMetaData.AddMetadata(linkTableNavProp.Name, assoc2);
        }

        #endregion
    }
}