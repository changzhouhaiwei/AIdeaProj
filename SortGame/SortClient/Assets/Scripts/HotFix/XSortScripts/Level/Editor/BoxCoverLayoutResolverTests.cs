#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FishXSortPlay.Tests
{
    public class BoxCoverLayoutResolverTests
    {
        private readonly List<LevelModel> createdLevels = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdLevels.Count; i++)
            {
                if (createdLevels[i] != null)
                {
                    Object.DestroyImmediate(createdLevels[i]);
                }
            }

            createdLevels.Clear();
        }

        [Test]
        public void Validate_AcceptsNonOverlappingCandidates()
        {
            LevelModel level = CreateLevel(
                new Vector2Int(6, 6),
                new Vector2Int(6, 6));
            Layer editableLayer = level.GetLayer(1);

            Configure(
                editableLayer,
                BoxCoverSize.Size2x2,
                1,
                new Vector2Int(0, 0));
            Configure(
                editableLayer,
                BoxCoverSize.Size2x3,
                1,
                new Vector2Int(3, 0));

            List<string> errors = BoxCoverLayoutResolver.Validate(level);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void Size2x3_UsesVerticalTwoWideThreeHighFootprint()
        {
            Assert.That(
                BoxCoverSizeUtility.GetCellSize(BoxCoverSize.Size2x3),
                Is.EqualTo(new Vector2Int(2, 3)));
        }

        [Test]
        public void Validate_ReportsTopLayerOutOfBoundsAndExcessCount()
        {
            LevelModel level = CreateLevel(
                new Vector2Int(4, 4),
                new Vector2Int(4, 4));
            Configure(
                level.GetLayer(0),
                BoxCoverSize.Size2x2,
                1,
                new Vector2Int(0, 0));
            Configure(
                level.GetLayer(1),
                BoxCoverSize.Size3x3,
                2,
                new Vector2Int(2, 2));

            List<string> errors = BoxCoverLayoutResolver.Validate(level);

            Assert.That(
                errors.Any(error => error.Contains("Layer 1") && error.Contains("top")),
                Is.True,
                string.Join("\n", errors));
            Assert.That(
                errors.Any(error => error.Contains("Layer 2") && error.Contains("outside")),
                Is.True,
                string.Join("\n", errors));
            Assert.That(
                errors.Any(error => error.Contains("Layer 2") && error.Contains("cannot place 2")),
                Is.True,
                string.Join("\n", errors));
        }

        [Test]
        public void Validate_ReportsCrossSizeOverlapOnItsLayer()
        {
            LevelModel level = CreateLevel(
                new Vector2Int(6, 6),
                new Vector2Int(6, 6));
            Layer editableLayer = level.GetLayer(1);
            Configure(
                editableLayer,
                BoxCoverSize.Size2x2,
                1,
                new Vector2Int(0, 0));
            Configure(
                editableLayer,
                BoxCoverSize.Size2x3,
                1,
                new Vector2Int(1, 1));

            List<string> errors = BoxCoverLayoutResolver.Validate(level);

            Assert.That(
                errors.Any(error => error.Contains("Layer 2") && error.Contains("different sizes")),
                Is.True,
                string.Join("\n", errors));
        }

        [Test]
        public void Resolve_SelectsRequestedNonOverlappingSameSizeCandidates()
        {
            LevelModel level = CreateLevel(
                new Vector2Int(4, 2),
                new Vector2Int(4, 2));
            Configure(
                level.GetLayer(1),
                BoxCoverSize.Size2x2,
                2,
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0));

            List<BoxCoverResolvedPlacement> placements =
                BoxCoverLayoutResolver.Resolve(level, 0, 99);

            Assert.That(placements.Count, Is.EqualTo(2));
            Assert.That(
                placements.Select(item => item.Anchor),
                Is.EquivalentTo(new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(2, 0),
                }));
        }

        [Test]
        public void Resolve_ReductionRemovesBottomFirstAndKeepsAtLeastTwo()
        {
            LevelModel level = CreateLevel(
                new Vector2Int(6, 6),
                new Vector2Int(6, 6),
                new Vector2Int(6, 6));
            Configure(
                level.GetLayer(1),
                BoxCoverSize.Size2x2,
                2,
                new Vector2Int(0, 0),
                new Vector2Int(3, 0));
            Configure(
                level.GetLayer(2),
                BoxCoverSize.Size2x2,
                2,
                new Vector2Int(0, 0),
                new Vector2Int(3, 0));

            List<BoxCoverResolvedPlacement> reducedOnce =
                BoxCoverLayoutResolver.Resolve(level, 1, 1234);
            Assert.That(reducedOnce.Count, Is.EqualTo(3));
            Assert.That(reducedOnce.Count(item => item.layer == 1), Is.EqualTo(2));
            Assert.That(reducedOnce.Count(item => item.layer == 2), Is.EqualTo(1));

            List<BoxCoverResolvedPlacement> reducedAsFarAsPossible =
                BoxCoverLayoutResolver.Resolve(level, 99, 1234);
            Assert.That(reducedAsFarAsPossible.Count, Is.EqualTo(2));
            Assert.That(reducedAsFarAsPossible.All(item => item.layer == 1), Is.True);
        }

        private LevelModel CreateLevel(params Vector2Int[] layerSizes)
        {
            LevelModel level = ScriptableObject.CreateInstance<LevelModel>();
            Layer[] layers = new Layer[layerSizes.Length];
            for (int i = 0; i < layerSizes.Length; i++)
            {
                layers[i] = CreateLayer(layerSizes[i].x, layerSizes[i].y);
            }

            SetPrivateField(level, "layers", layers);
            createdLevels.Add(level);
            return level;
        }

        private static Layer CreateLayer(int width, int height)
        {
            Layer layer = new Layer();
            LayerRow[] rows = new LayerRow[height];
            for (int y = 0; y < height; y++)
            {
                LayerRow row = new LayerRow();
                CellModel[] cells = new CellModel[width];
                for (int x = 0; x < width; x++)
                {
                    cells[x] = new CellModel();
                }

                SetPrivateField(row, "cells", cells);
                rows[y] = row;
            }

            SetPrivateField(layer, "rows", rows);
            return layer;
        }

        private static void Configure(
            Layer layer,
            BoxCoverSize size,
            int count,
            params Vector2Int[] anchors)
        {
            BoxCoverPlacementData configuration = layer.GetBoxCoverConfiguration(size);
            for (int i = 0; i < anchors.Length; i++)
            {
                configuration.ToggleAnchor(anchors[i]);
            }

            configuration.SetCount(count);
        }

        private static void SetPrivateField<TTarget, TValue>(
            TTarget target,
            string fieldName,
            TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {typeof(TTarget).Name}.{fieldName}");
            field.SetValue(target, value);
        }
    }
}
#endif
