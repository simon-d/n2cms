using System;
using System.Collections.Generic;
using NUnit.Framework;
using N2.Definitions;
using N2.Details;
using N2.Integrity;
using N2.Tests.Definitions.Definitions;
using N2.Tests.Definitions.Definitions.Details;

namespace N2.Tests.Definitions
{
	[TestFixture]
	public class DefinitionFixture : PersistenceAwareBase
	{
		[Test]
		public void DefinitionManagerHasCorrectNumberOfDefinitions()
		{
			Assert.Greater(engine.Definitions.GetDefinitions().Count, 9);
		}

		[Test]
		public void DefinitoinsCanBeRetrievedByString()
		{
			ItemDefinition definitionByType = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			ItemDefinition definitionByString = engine.Definitions.GetDefinition("ItemWithDetails");

			Assert.AreSame(definitionByType, definitionByString);
		}
		[Test]
		public void GetDefinitionByUnknownTypeReturnsNull()
		{
			ItemDefinition shouldBeNull = engine.Definitions.GetDefinition("IsNotDefined");
			Assert.IsNull(shouldBeNull);
		}


		[Test]
		public void ItemHasCorrectNumberOfDetails()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual(4, definition.Editables.Count);
		}

		[Test]
		public void ItemContainsCorrectTypesOfDetails()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			List<Type> expected = new List<Type>();
			expected.Add(typeof (EditableAttribute));
			expected.Add(typeof (CustomizedEditableAttribute));
			expected.Add(typeof (EditableTextBoxAttribute));
			expected.Add(typeof (EditableFreeTextAreaAttribute));

			foreach (IEditable a in definition.Editables)
			{
				Assert.IsTrue(expected.Contains(a.GetType()));
				expected.Remove(a.GetType());
			}
			Assert.AreEqual(0, expected.Count);
		}

		[Test]
		public void AllowedChildrenAreCorrect()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.IsNotNull(definition);
			EnumerableAssert.Contains(definition.AllowedChildren, engine.Definitions.GetDefinition(typeof(ItemInZone1Or2)));
		}

		[Test]
		public void AvailableZonesAreCorrect()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual(2, definition.AvailableZones.Count);
			EnumerableAssert.Contains(definition.AvailableZones, new AvailableZoneAttribute("Zone1", "Zone1"));
			EnumerableAssert.Contains(definition.AvailableZones, new AvailableZoneAttribute("Zone2", "Zone2"));
		}

		[Test]
		public void AvailableZonesAreInherited()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (SubItemWithDetails));
			Assert.AreEqual(3, definition.AvailableZones.Count);
			EnumerableAssert.Contains(definition.AvailableZones, new AvailableZoneAttribute("Zone1", "Zone1"));
			EnumerableAssert.Contains(definition.AvailableZones, new AvailableZoneAttribute("Zone2", "Zone2"));
			EnumerableAssert.Contains(definition.AvailableZones, new AvailableZoneAttribute("Zone3", "Zone3"));
		}

		[Test]
		public void AlowedZonesAreDefined()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemInZone1Or2));
			Assert.AreEqual(2, definition.AllowedZoneNames.Count);
			EnumerableAssert.Contains(definition.AllowedZoneNames, "Zone1");
			EnumerableAssert.Contains(definition.AllowedZoneNames, "Zone2");
			Assert.AreNotEqual(definition.AllowedZoneNames[0], definition.AllowedZoneNames[1]);
		}

		[Test]
		public void TitleIsCorrect()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual("Item With Details", definition.Title);
		}

		[Test]
		public void DiscriminatorIsCorrect()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual("ItemWithDetails", definition.Discriminator);
		}

		[Test]
		public void ToolTipIsCorrect()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual("Detailed item tooltip", definition.ToolTip);
		}

		[Test]
		public void SortOrderIsCorrect()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual(123, definition.SortOrder);
		}

		[Test]
		public void ItemTypeIsCorrect()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual(typeof (ItemWithDetails), definition.ItemType);
		}

		[Test]
		public void Definition_UsesDefinedIconUrl_NotRuntimeIconUrl()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual("/N2/Resources/icons/page.png", definition.IconUrl);
		}

		[Test]
		public void CanCreateInstanceWithNullParent()
		{
			ContentItem item = engine.Definitions.CreateInstance(typeof (ItemWithDetails), null);
			Assert.AreEqual(typeof (ItemWithDetails), item.GetContentType());
		}

		[Test]
		public void CanCreateInstanceWithAllowedParent()
		{
			ContentItem item1 = engine.Definitions.CreateInstance(typeof(ItemWithDetails), null);
			ContentItem item2 = engine.Definitions.CreateInstance(typeof(ItemInZone1Or2), item1);
			Assert.AreEqual(typeof (ItemWithDetails), item1.GetContentType());
			Assert.AreEqual(typeof (ItemInZone1Or2), item2.GetContentType());
		}

		[Test]
		public void HasCorrectNumberOfDisplayableAttributes()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.That(definition.Displayables.Count, Is.EqualTo(14));
		}

		[Test]
		public void HasCorrectNumberOfEditorModifiers()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual(2, definition.Modifiers.Count);
		}

		[Test]
		public void HasCorrectNumberOfEditorModifiersOnProperty()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			Assert.AreEqual(1, definition.GetModifiers("TestProperty1").Count);
		}

		[Test]
		public void IsChildAllowedWorksAsExpected()
		{
			ItemDefinition parentDefinition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			ItemDefinition childDefinition1 = engine.Definitions.GetDefinition(typeof (ItemInZone1Or2));
			ItemDefinition childDefinition2 = engine.Definitions.GetDefinition(typeof (SideshowItem));
			Assert.IsTrue(parentDefinition.IsChildAllowed(childDefinition1));
			Assert.IsFalse(parentDefinition.IsChildAllowed(childDefinition2));
		}

		[Test]
		public void IsChildTypeAllowedWorksAsExpected()
		{
			ItemDefinition parentDefinition = engine.Definitions.GetDefinition(typeof (ItemWithDetails));
			EnumerableAssert.Contains(parentDefinition.AllowedChildren, engine.Definitions.GetDefinition(typeof (ItemInZone1Or2)));
			EnumerableAssert.DoesntContain(parentDefinition.AllowedChildren, engine.Definitions.GetDefinition(typeof (SideshowItem)));
			//Assert.IsTrue(parentDefinition.IsChildTypeAllowed());
			//Assert.IsFalse(parentDefinition.IsChildTypeAllowed(typeof(N2.Tests.Definitions.Definitions.)));
		}

		[Test]
		public void IsZoneAllowedWorksAsExpected()
		{
			ItemDefinition definition = engine.Definitions.GetDefinition(typeof (ItemInZone1Or2));
			Assert.IsTrue(definition.IsAllowedInZone("Zone1"));
			Assert.IsTrue(definition.IsAllowedInZone("Zone2"));
			Assert.IsFalse(definition.IsAllowedInZone("Zone3"));
		}
	}
}