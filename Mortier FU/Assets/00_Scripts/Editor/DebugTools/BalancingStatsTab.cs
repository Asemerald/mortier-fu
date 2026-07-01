using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MortierFu.Editor
{
    public sealed class BalancingStatsTab
    {
        private enum ViewMode
        {
            Detailed,
            Compare
        }

        private const double k_refreshInterval = 0.1d;
        private const float k_detailStatNameWidth = 230f;
        private const float k_detailBaseWidth = 80f;
        private const float k_detailValueWidth = 90f;
        private const float k_detailDeltaWidth = 130f;
        private const float k_detailModsWidth = 70f;

        private const float k_compareStatNameWidth = 240f;
        private const float k_comparePlayerColumnWidth = 185f;

        private readonly VisualElement _root;
        private readonly List<StatMetric> _metrics = new();

        private readonly Dictionary<PlayerCharacter, PlayerStatsView> _detailedViewsByCharacter = new();
        private readonly List<CompareRowView> _compareRows = new();

        private ScrollView _scrollView;
        private VisualElement _content;

        private ViewMode _viewMode = ViewMode.Detailed;
        private double _nextRefreshTime;
        private bool _isSubscribedToEditorUpdate;
        private bool _needsRebuild = true;

        public BalancingStatsTab(VisualElement root)
        {
            _root = root;
            CreateMetrics();
        }

        public void Show()
        {
            RebuildRoot();

            if (_isSubscribedToEditorUpdate)
                return;

            EditorApplication.update += EditorUpdate;
            _isSubscribedToEditorUpdate = true;
        }

        public void Dispose()
        {
            if (_isSubscribedToEditorUpdate)
            {
                EditorApplication.update -= EditorUpdate;
                _isSubscribedToEditorUpdate = false;
            }

            DisposeRuntimeViews();
        }

        private void EditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextRefreshTime)
                return;

            _nextRefreshTime = EditorApplication.timeSinceStartup + k_refreshInterval;

            RefreshPlayers(forceRebuild: false);
        }

        private void RebuildRoot()
        {
            DisposeRuntimeViews();

            _root.Clear();

            BuildHeader();
            BuildViewModeControls();
            BuildScrollView();

            _needsRebuild = true;
            RefreshPlayers(forceRebuild: true);
        }

        private void BuildHeader()
        {
            Label title = new ("Balancing - Live Player Stats")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 18,
                    marginBottom = 4
                }
            };

            Label subtitle = new ("Live runtime stats. Values highlight when they change.")
            {
                style =
                {
                    color = new Color(0.7f, 0.7f, 0.7f),
                    marginBottom = 8
                }
            };

            _root.Add(title);
            _root.Add(subtitle);
        }

        private void BuildViewModeControls()
        {
            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 10
                }
            };

            Button detailedButton = new Button(() =>
            {
                if (_viewMode == ViewMode.Detailed)
                    return;

                _viewMode = ViewMode.Detailed;
                RebuildRoot();
            })
            {
                text = "Detailed View"
            };

            Button compareButton = new Button(() =>
            {
                if (_viewMode == ViewMode.Compare)
                    return;

                _viewMode = ViewMode.Compare;
                RebuildRoot();
            })
            {
                text = "Compare View"
            };

            StyleModeButton(detailedButton, _viewMode == ViewMode.Detailed);
            StyleModeButton(compareButton, _viewMode == ViewMode.Compare);

            row.Add(detailedButton);
            row.Add(compareButton);

            _root.Add(row);
        }

        private static void StyleModeButton(Button button, bool active)
        {
            button.style.marginRight = 6;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.paddingTop = 4;
            button.style.paddingBottom = 4;
            button.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;

            if (active)
                button.style.backgroundColor = new Color(0.25f, 0.35f, 0.55f);
        }

        private void BuildScrollView()
        {
            _root.style.flexGrow = 1f;
            _root.style.minHeight = 0;

            _scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                style =
                {
                    flexGrow = 1f,
                    flexShrink = 1f,
                    minHeight = 0
                }
            };

            _scrollView.contentContainer.style.flexDirection = FlexDirection.Column;
            _scrollView.contentContainer.style.flexGrow = 0f;
            _scrollView.contentContainer.style.flexShrink = 0f;

            _content = new VisualElement
            {
                style =
                {
                    flexGrow = 0f,
                    flexShrink = 0f,
                    minWidth = 760
                }
            };

            _scrollView.contentContainer.Add(_content);
            _root.Add(_scrollView);
        }

        private void RefreshPlayers(bool forceRebuild)
        {
            if (!Application.isPlaying)
            {
                ShowPlayModeMessage();
                return;
            }

            var characters = CollectRuntimeCharacters();

            bool requiresRebuild =
                forceRebuild ||
                _needsRebuild ||
                HasDifferentCharacters(characters);

            if (requiresRebuild)
            {
                if (_viewMode == ViewMode.Detailed)
                    RebuildDetailedView(characters);
                else
                    RebuildCompareView(characters);

                _needsRebuild = false;
            }

            if (_viewMode == ViewMode.Detailed)
            {
                foreach (PlayerStatsView view in _detailedViewsByCharacter.Values)
                    view.Refresh();
            }
            else
            {
                for (int i = 0; i < _compareRows.Count; i++)
                    _compareRows[i].Refresh();
            }
        }

        private void ShowPlayModeMessage()
        {
            DisposeRuntimeViews();

            _content?.Clear();

            if (_content == null)
                return;

            if (_content.Q<Label>("play-mode-message") != null)
                return;

            _content.Add(new Label("Enter Play Mode to inspect live player stats.")
            {
                name = "play-mode-message",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Italic,
                    color = new Color(0.8f, 0.8f, 0.8f),
                    marginTop = 8
                }
            });
        }

        private void RebuildDetailedView(List<PlayerCharacter> characters)
        {
            DisposeRuntimeViews();

            _content.Clear();
            _content.style.minWidth = 760f;
            
            if (characters.Count == 0)
            {
                _content.Add(MakeEmptyState());
                return;
            }

            for (int i = 0; i < characters.Count; i++)
            {
                PlayerStatsView view = new (characters[i], _metrics);
                _detailedViewsByCharacter.Add(characters[i], view);
                _content.Add(view.Root);
            }
        }

        private void RebuildCompareView(List<PlayerCharacter> characters)
        {
            DisposeRuntimeViews();

            _content.Clear();
            _content.style.minWidth = k_compareStatNameWidth + characters.Count * k_comparePlayerColumnWidth + 40f;
            
            if (characters.Count == 0)
            {
                _content.Add(MakeEmptyState());
                return;
            }

            _content.Add(MakeCompareHeader(characters));
            AddCompareSection("Character Statistics", characters, "Character");
            AddCompareSection("Mortar Statistics", characters, "Mortar");
            AddCompareSection("Dash / Strike Statistics", characters, "Dash");
            AddCompareSection("Computed Gameplay Values", characters, "Computed");
        }

        private void AddCompareSection(string sectionTitle, List<PlayerCharacter> characters, string sectionKey)
        {
            _content.Add(MakeSectionTitle(sectionTitle));

            for (int i = 0; i < _metrics.Count; i++)
            {
                StatMetric metric = _metrics[i];

                if (metric.Section != sectionKey)
                    continue;

                CompareRowView row = new (metric, characters);
                _compareRows.Add(row);
                _content.Add(row.Root);
            }
        }

        private bool HasDifferentCharacters(List<PlayerCharacter> characters)
        {
            if (_viewMode == ViewMode.Detailed)
            {
                if (characters.Count != _detailedViewsByCharacter.Count)
                    return true;

                for (int i = 0; i < characters.Count; i++)
                {
                    if (!_detailedViewsByCharacter.ContainsKey(characters[i]))
                        return true;
                }

                return false;
            }

            return _compareRows.Count == 0 && characters.Count > 0;
        }

        private void DisposeRuntimeViews()
        {
            foreach (PlayerStatsView view in _detailedViewsByCharacter.Values)
                view.Dispose();

            _detailedViewsByCharacter.Clear();
            _compareRows.Clear();
        }

        private static List<PlayerCharacter> CollectRuntimeCharacters()
        {
            var result = new List<PlayerCharacter>();

            LobbyService lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            if (lobbyService != null)
            {
                var players = lobbyService.GetPlayers();

                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];

                    if (!player || !player.Character)
                        continue;

                    if (!result.Contains(player.Character))
                        result.Add(player.Character);
                }
            }

            var allCharacters = Object.FindObjectsByType<PlayerCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < allCharacters.Length; i++)
            {
                PlayerCharacter character = allCharacters[i];

                if (!character)
                    continue;

                if (!result.Contains(character))
                    result.Add(character);
            }

            result.Sort((a, b) =>
            {
                int aIndex = a.Owner ? a.Owner.PlayerIndex : int.MaxValue;
                int bIndex = b.Owner ? b.Owner.PlayerIndex : int.MaxValue;
                return aIndex.CompareTo(bIndex);
            });

            return result;
        }

        private static VisualElement MakeEmptyState()
        {
            return new Label("No spawned PlayerCharacter found.")
            {
                style =
                {
                    color = new Color(0.8f, 0.8f, 0.8f),
                    marginTop = 8
                }
            };
        }

        private static VisualElement MakeSectionTitle(string title)
        {
            return new Label(title)
            {
                style =
                {
                    height = 24,
                    flexShrink = 0f,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginTop = 8,
                    marginBottom = 2,
                    color = new Color(0.85f, 0.85f, 0.85f)
                }
            };
        }

        private static VisualElement MakeCompareHeader(List<PlayerCharacter> characters)
        {
            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    height = 34,
                    flexShrink = 0f,
                    alignItems = Align.Center,
                    backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                    marginBottom = 6,
                    paddingLeft = 6,
                    paddingRight = 6,
                    minWidth = k_compareStatNameWidth + characters.Count * k_comparePlayerColumnWidth
                }
            };

            row.Add(new Label("Stat")
            {
                style =
                {
                    minWidth = k_compareStatNameWidth,
                    maxWidth = k_compareStatNameWidth,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white
                }
            });

            for (int i = 0; i < characters.Count; i++)
            {
                PlayerCharacter character = characters[i];
                int playerIndex = character.Owner ? character.Owner.PlayerIndex + 1 : 0;

                row.Add(new Label(playerIndex > 0 ? $"P{playerIndex}" : "P?")
                {
                    style =
                    {
                        minWidth = k_comparePlayerColumnWidth,
                        maxWidth = k_comparePlayerColumnWidth,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = Color.white
                    }
                });
            }

            return row;
        }

        private void CreateMetrics()
        {
            _metrics.Clear();

            AddRaw("Character", "Max Health", s => s.MaxHealth);
            AddRaw("Character", "Move Speed", s => s.MoveSpeed);
            AddRaw("Character", "Acceleration", s => s.Accel);
            AddRaw("Character", "Deceleration", s => s.Decel);
            AddRaw("Character", "Avatar Size", s => s.AvatarSize);

            AddRaw("Mortar", "Bombshell Damage", s => s.BombshellDamage);
            AddRaw("Mortar", "Bombshell Size", s => s.BombshellSize);
            AddRaw("Mortar", "Impact Radius", s => s.BombshellImpactRadius);
            AddRaw("Mortar", "Bombshell Bounces", s => s.BombshellBounces);
            AddRaw("Mortar", "Fire Rate", s => s.FireRate);
            AddRaw("Mortar", "Shot Range", s => s.ShotRange);
            AddRaw("Mortar", "Bombshell Speed", s => s.BombshellSpeed);
            AddRaw("Mortar", "Aim Widget Speed", s => s.AimWidgetSpeed);

            AddRaw("Dash", "Strike Damage", s => s.StrikeDamage);
            AddRaw("Dash", "Dash Charges", s => s.DashCharges);
            AddRaw("Dash", "Dash Cooldown", s => s.DashCooldown);
            AddRaw("Dash", "Dash Duration", s => s.DashDuration);
            AddRaw("Dash", "Strike Radius", s => s.StrikeRadius);
            AddRaw("Dash", "Strike Push Force", s => s.StrikePushForce);
            AddRaw("Dash", "Strike Knockback Duration", s => s.StrikeKnockbackDuration);

            AddComputed("Computed", "Real Fire Cooldown", s => s.GetFireRate());
            AddComputed("Computed", "Real Bombshell Speed", s => s.GetBombshellSpeed());
            AddComputed("Computed", "Real Avatar Size", s => s.GetAvatarSize());
            AddComputed("Computed", "Real Shot Range", s => s.GetShotRange());
            AddComputed("Computed", "Real Bombshell Size", s => s.GetBombshellSize());
            AddComputed("Computed", "Real Dash Cooldown", s => s.GetDashCooldown());
            AddComputed("Computed", "Real Dash Push Force", s => s.GetDashPushForce());
            AddComputed("Computed", "Real Strike Radius", s => s.GetStrikeRadius());
            AddComputed("Computed", "Real Knockback Stun Duration", s => s.GetKnockbackStunDuration());
        }

        private void AddRaw(string section, string label, Func<SO_CharacterStats, CharacterStat> getter)
        {
            _metrics.Add(StatMetric.Raw(section, label, getter));
        }

        private void AddComputed(string section, string label, Func<SO_CharacterStats, float> getter)
        {
            _metrics.Add(StatMetric.Computed(section, label, getter));
        }

        private sealed class PlayerStatsView : IDisposable
        {
            public VisualElement Root { get; }

            private readonly PlayerCharacter _character;
            private readonly List<StatRowView> _statRows = new();
            private readonly List<ComputedStatRowView> _computedRows = new();

            public PlayerStatsView(PlayerCharacter character, List<StatMetric> metrics)
            {
                _character = character;
                Root = new VisualElement();

                Build(metrics);
                Subscribe();
            }

            public void Dispose()
            {
                Unsubscribe();
            }

            public void Refresh()
            {
                for (int i = 0; i < _statRows.Count; i++)
                    _statRows[i].Refresh();

                for (int i = 0; i < _computedRows.Count; i++)
                    _computedRows[i].Refresh();
            }

            private void Build(List<StatMetric> metrics)
            {
                Root.style.flexShrink = 0f;
                Root.style.marginBottom = 10f;
                Root.style.minWidth = 760f;

                int playerIndex = _character.Owner ? _character.Owner.PlayerIndex + 1 : 0;

                Foldout foldout = new Foldout
                {
                    text = playerIndex > 0
                        ? $"Player {playerIndex} - {_character.name}"
                        : $"Player ? - {_character.name}",
                    value = true,
                    style =
                    {
                        flexShrink = 0f,
                        marginBottom = 8,
                        paddingTop = 4,
                        paddingBottom = 4,
                        paddingLeft = 6,
                        paddingRight = 6,
                        borderBottomWidth = 1,
                        borderTopWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                        borderBottomColor = new Color(0.25f, 0.25f, 0.25f),
                        borderTopColor = new Color(0.25f, 0.25f, 0.25f),
                        borderLeftColor = new Color(0.25f, 0.25f, 0.25f),
                        borderRightColor = new Color(0.25f, 0.25f, 0.25f),
                        backgroundColor = new Color(0.13f, 0.13f, 0.13f)
                    }
                };

                Root.Add(foldout);

                VisualElement content = new VisualElement
                {
                    style =
                    {
                        flexShrink = 0f,
                        minWidth = 730f,
                        paddingTop = 6,
                        paddingBottom = 6,
                        paddingLeft = 8,
                        paddingRight = 8
                    }
                };

                foldout.Add(content);

                content.Add(MakeColumnHeader());

                AddDetailedSection(content, metrics, "Character", "Character Statistics");
                AddDetailedSection(content, metrics, "Mortar", "Mortar Statistics");
                AddDetailedSection(content, metrics, "Dash", "Dash / Strike Statistics");
                AddDetailedSection(content, metrics, "Computed", "Computed Gameplay Values");
            }

            private void AddDetailedSection(VisualElement parent, List<StatMetric> metrics, string sectionKey, string sectionTitle)
            {
                parent.Add(MakeSectionTitle(sectionTitle));

                for (int i = 0; i < metrics.Count; i++)
                {
                    StatMetric metric = metrics[i];

                    if (metric.Section != sectionKey)
                        continue;

                    if (metric.IsComputed)
                    {
                        ComputedStatRowView computedRow = new (metric, _character);
                        _computedRows.Add(computedRow);
                        parent.Add(computedRow.Root);
                    }
                    else
                    {
                        StatRowView statRow = new (metric.Label, metric.GetStat(_character));
                        _statRows.Add(statRow);
                        parent.Add(statRow.Root);
                    }
                }
            }

            private static VisualElement MakeColumnHeader()
            {
                VisualElement row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        height = 26,
                        flexShrink = 0f,
                        alignItems = Align.Center,
                        backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                        marginBottom = 6,
                        paddingTop = 2,
                        paddingBottom = 2
                    }
                };

                row.Add(MakeHeaderCell("Stat", k_detailStatNameWidth, TextAnchor.MiddleLeft));
                row.Add(MakeHeaderCell("Base", k_detailBaseWidth, TextAnchor.MiddleRight));
                row.Add(MakeHeaderCell("Value", k_detailValueWidth, TextAnchor.MiddleRight));
                row.Add(MakeHeaderCell("Δ Base", k_detailDeltaWidth, TextAnchor.MiddleRight));
                row.Add(MakeHeaderCell("Mods", k_detailModsWidth, TextAnchor.MiddleRight));

                return row;
            }

            private static Label MakeHeaderCell(string text, float width, TextAnchor alignment)
            {
                return new Label(text)
                {
                    style =
                    {
                        minWidth = width,
                        unityTextAlign = alignment,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = Color.white
                    }
                };
            }

            private void Subscribe()
            {
                for (int i = 0; i < _statRows.Count; i++)
                    _statRows[i].Subscribe();
            }

            private void Unsubscribe()
            {
                for (int i = 0; i < _statRows.Count; i++)
                    _statRows[i].Unsubscribe();
            }
        }

        private sealed class StatRowView
        {
            private const double k_highlightDuration = 1.2d;

            public VisualElement Root { get; }

            private readonly string _label;
            private readonly CharacterStat _stat;

            private readonly Label _baseLabel;
            private readonly Label _valueLabel;
            private readonly Label _deltaLabel;
            private readonly Label _modifierCountLabel;
            private readonly VisualElement _detailsContainer;

            private float _lastValue = float.NaN;
            private int _lastModifierCount = -1;
            private double _highlightUntil;

            public StatRowView(string label, CharacterStat stat)
            {
                _label = label;
                _stat = stat;

                Root = new VisualElement();

                VisualElement header = MakeRow();
                header.RegisterCallback<MouseDownEvent>(_ => ToggleDetails());

                header.Add(MakeNameCell(label));

                _baseLabel = MakeCell(k_detailBaseWidth);
                _valueLabel = MakeCell(k_detailValueWidth);
                _deltaLabel = MakeCell(k_detailDeltaWidth);
                _modifierCountLabel = MakeCell(k_detailModsWidth);

                header.Add(_baseLabel);
                header.Add(_valueLabel);
                header.Add(_deltaLabel);
                header.Add(_modifierCountLabel);

                _detailsContainer = new VisualElement
                {
                    style =
                    {
                        display = DisplayStyle.None,
                        marginLeft = 18,
                        marginTop = 3,
                        marginBottom = 5,
                        paddingLeft = 8,
                        paddingTop = 4,
                        paddingBottom = 4,
                        backgroundColor = new Color(0.1f, 0.1f, 0.1f)
                    }
                };

                Root.Add(header);
                Root.Add(_detailsContainer);

                Refresh();
            }

            public void Subscribe()
            {
                if (_stat != null)
                    _stat.OnDirtyUpdated += MarkRecentChange;
            }

            public void Unsubscribe()
            {
                if (_stat != null)
                    _stat.OnDirtyUpdated -= MarkRecentChange;
            }

            public void Refresh()
            {
                if (_stat == null)
                    return;

                float baseValue = _stat.BaseValue;
                float value = _stat.Value;
                float delta = value - baseValue;
                int modifierCount = _stat.StatModifiers.Count;

                bool changed =
                    !float.IsNaN(_lastValue) &&
                    (!Mathf.Approximately(value, _lastValue) || modifierCount != _lastModifierCount);

                if (changed)
                    MarkRecentChange();

                _baseLabel.text = Format(baseValue);
                _valueLabel.text = Format(value);
                _deltaLabel.text = FormatDelta(baseValue, value);
                _modifierCountLabel.text = modifierCount.ToString();

                bool isRecentlyChanged = EditorApplication.timeSinceStartup < _highlightUntil;

                _valueLabel.style.color = isRecentlyChanged
                    ? new Color(1f, 0.85f, 0.25f)
                    : new Color(0.85f, 0.85f, 0.85f);

                _deltaLabel.style.color = GetDeltaColor(delta);

                _lastValue = value;
                _lastModifierCount = modifierCount;

                RebuildModifierDetails();
            }

            private void ToggleDetails()
            {
                _detailsContainer.style.display =
                    _detailsContainer.style.display == DisplayStyle.None
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }

            private void MarkRecentChange()
            {
                _highlightUntil = EditorApplication.timeSinceStartup + k_highlightDuration;
            }

            private void RebuildModifierDetails()
            {
                _detailsContainer.Clear();

                if (_stat.StatModifiers.Count == 0)
                {
                    _detailsContainer.Add(new Label("No active modifiers.")
                    {
                        style =
                        {
                            color = new Color(0.65f, 0.65f, 0.65f),
                            unityFontStyleAndWeight = FontStyle.Italic
                        }
                    });

                    return;
                }

                _detailsContainer.Add(MakeModifierHeader());

                for (int i = 0; i < _stat.StatModifiers.Count; i++)
                {
                    StatModifier modifier = _stat.StatModifiers[i];
                    _detailsContainer.Add(MakeModifierRow(modifier));
                }
            }

            private static VisualElement MakeModifierHeader()
            {
                VisualElement row = MakeRow();
                row.style.marginBottom = 2;

                row.Add(MakeMiniHeaderCell("Source", 180));
                row.Add(MakeMiniHeaderCell("Type", 110));
                row.Add(MakeMiniHeaderCell("Value", 90));
                row.Add(MakeMiniHeaderCell("Order", 60));

                return row;
            }

            private static VisualElement MakeModifierRow(StatModifier modifier)
            {
                VisualElement row = MakeRow();

                string sourceName = modifier.Source != null
                    ? modifier.Source.GetType().Name
                    : "Unknown";

                row.Add(MakeMiniCell(sourceName, 180));
                row.Add(MakeMiniCell(modifier.Type.ToString(), 110));
                row.Add(MakeMiniCell(FormatModifierValue(modifier), 90));
                row.Add(MakeMiniCell(modifier.Order.ToString(), 60));

                return row;
            }

            private static VisualElement MakeRow()
            {
                return new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        height = 24,
                        flexShrink = 0f,
                        alignItems = Align.Center
                    }
                };
            }

            private static Label MakeNameCell(string text)
            {
                return new Label(text)
                {
                    style =
                    {
                        minWidth = k_detailStatNameWidth,
                        maxWidth = k_detailStatNameWidth,
                        color = new Color(0.78f, 0.78f, 0.78f)
                    }
                };
            }

            private static Label MakeCell(float width)
            {
                return new Label
                {
                    style =
                    {
                        minWidth = width,
                        maxWidth = width,
                        unityTextAlign = TextAnchor.MiddleRight,
                        color = new Color(0.85f, 0.85f, 0.85f)
                    }
                };
            }

            private static Label MakeMiniHeaderCell(string text, float width)
            {
                return new Label(text)
                {
                    style =
                    {
                        minWidth = width,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = new Color(0.75f, 0.75f, 0.75f)
                    }
                };
            }

            private static Label MakeMiniCell(string text, float width)
            {
                return new Label(text)
                {
                    style =
                    {
                        minWidth = width,
                        color = new Color(0.72f, 0.72f, 0.72f)
                    }
                };
            }
        }

        private sealed class ComputedStatRowView
        {
            private const double k_highlightDuration = 1.2d;

            public VisualElement Root { get; }

            private readonly StatMetric _metric;
            private readonly PlayerCharacter _character;

            private readonly Label _valueLabel;

            private float _lastValue = float.NaN;
            private double _highlightUntil;

            public ComputedStatRowView(StatMetric metric, PlayerCharacter character)
            {
                _metric = metric;
                _character = character;

                Root = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        height = 24,
                        flexShrink = 0f,
                        alignItems = Align.Center
                    }
                };

                Root.Add(new Label(metric.Label)
                {
                    style =
                    {
                        minWidth = k_detailStatNameWidth,
                        maxWidth = k_detailStatNameWidth,
                        color = new Color(0.68f, 0.85f, 1f)
                    }
                });

                Root.Add(new Label("-")
                {
                    style =
                    {
                        minWidth = k_detailBaseWidth,
                        maxWidth = k_detailBaseWidth,
                        unityTextAlign = TextAnchor.MiddleRight,
                        color = new Color(0.45f, 0.45f, 0.45f)
                    }
                });

                _valueLabel = new Label
                {
                    style =
                    {
                        minWidth = k_detailValueWidth,
                        maxWidth = k_detailValueWidth,
                        unityTextAlign = TextAnchor.MiddleRight,
                        color = new Color(0.85f, 0.85f, 0.85f)
                    }
                };

                Root.Add(_valueLabel);

                Root.Add(new Label("-")
                {
                    style =
                    {
                        minWidth = k_detailDeltaWidth,
                        maxWidth = k_detailDeltaWidth,
                        unityTextAlign = TextAnchor.MiddleRight,
                        color = new Color(0.45f, 0.45f, 0.45f)
                    }
                });

                Root.Add(new Label("-")
                {
                    style =
                    {
                        minWidth = k_detailModsWidth,
                        maxWidth = k_detailModsWidth,
                        unityTextAlign = TextAnchor.MiddleRight,
                        color = new Color(0.45f, 0.45f, 0.45f)
                    }
                });

                Refresh();
            }

            public void Refresh()
            {
                float value = _metric.GetValue(_character);

                if (!float.IsNaN(_lastValue) && !Mathf.Approximately(value, _lastValue))
                    _highlightUntil = EditorApplication.timeSinceStartup + k_highlightDuration;

                _valueLabel.text = Format(value);

                _valueLabel.style.color = EditorApplication.timeSinceStartup < _highlightUntil
                    ? new Color(1f, 0.85f, 0.25f)
                    : new Color(0.85f, 0.85f, 0.85f);

                _lastValue = value;
            }
        }

        private sealed class CompareRowView
        {
            public VisualElement Root { get; }

            private readonly List<CompareCellView> _cells = new();

            public CompareRowView(StatMetric metric, List<PlayerCharacter> characters)
            {
                Root = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        height = metric.IsComputed ? 34 : 42,
                        flexShrink = 0f,
                        alignItems = Align.Center,
                        paddingLeft = 6,
                        paddingRight = 6,
                        minWidth = k_compareStatNameWidth + characters.Count * k_comparePlayerColumnWidth
                    }
                };

                Root.Add(new Label(metric.Label)
                {
                    style =
                    {
                        minWidth = k_compareStatNameWidth,
                        maxWidth = k_compareStatNameWidth,
                        color = metric.IsComputed
                            ? new Color(0.68f, 0.85f, 1f)
                            : new Color(0.78f, 0.78f, 0.78f)
                    }
                });

                for (int i = 0; i < characters.Count; i++)
                {
                    CompareCellView cell = new (metric, characters[i]);
                    _cells.Add(cell);
                    Root.Add(cell.Root);
                }
            }

            public void Refresh()
            {
                for (int i = 0; i < _cells.Count; i++)
                    _cells[i].Refresh();
            }
        }

        private sealed class CompareCellView
        {
            private const double k_highlightDuration = 1.2d;

            public VisualElement Root { get; }

            private readonly StatMetric _metric;
            private readonly PlayerCharacter _character;

            private readonly Label _valueLabel;
            private readonly Label _deltaLabel;

            private float _lastValue = float.NaN;
            private double _highlightUntil;

            public CompareCellView(StatMetric metric, PlayerCharacter character)
            {
                _metric = metric;
                _character = character;

                Root = new VisualElement
                {
                    style =
                    {
                        minWidth = k_comparePlayerColumnWidth,
                        maxWidth = k_comparePlayerColumnWidth,
                        flexShrink = 0f,
                        alignItems = Align.Center,
                        justifyContent = Justify.Center
                    }
                };

                _valueLabel = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = new Color(0.9f, 0.9f, 0.9f),
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                };

                _deltaLabel = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        fontSize = 10,
                        color = new Color(0.65f, 0.65f, 0.65f)
                    }
                };

                Root.Add(_valueLabel);
                Root.Add(_deltaLabel);

                Refresh();
            }

            public void Refresh()
            {
                float value = _metric.GetValue(_character);

                if (!float.IsNaN(_lastValue) && !Mathf.Approximately(value, _lastValue))
                    _highlightUntil = EditorApplication.timeSinceStartup + k_highlightDuration;

                _valueLabel.text = Format(value);

                if (_metric.IsComputed)
                {
                    _deltaLabel.text = "";
                }
                else
                {
                    CharacterStat stat = _metric.GetStat(_character);
                    _deltaLabel.text = stat != null ? FormatDeltaForSecondLine(stat.BaseValue, value) : "";
                    _deltaLabel.style.color = stat != null
                        ? GetDeltaColor(value - stat.BaseValue)
                        : new Color(0.65f, 0.65f, 0.65f);
                }

                bool highlighted = EditorApplication.timeSinceStartup < _highlightUntil;

                Root.style.backgroundColor = highlighted
                    ? new Color(0.28f, 0.23f, 0.08f)
                    : Color.clear;

                _valueLabel.style.color = highlighted
                    ? new Color(1f, 0.85f, 0.25f)
                    : new Color(0.9f, 0.9f, 0.9f);

                _lastValue = value;
            }
        }
        
        private static string FormatDeltaForSecondLine(float baseValue, float value)
        {
            float delta = value - baseValue;

            if (Mathf.Approximately(delta, 0f))
                return "";

            if (Mathf.Abs(baseValue) < 0.0001f)
                return delta > 0f ? $"+{Format(delta)}" : Format(delta);

            float percent = delta / baseValue * 100f;
            string deltaPrefix = delta > 0f ? "+" : "";
            string percentPrefix = percent > 0f ? "+" : "";

            return $"{deltaPrefix}{Format(delta)}  {percentPrefix}{percent:0.#}%";
        }
        
        private sealed class StatMetric
        {
            public string Section { get; }
            public string Label { get; }
            public bool IsComputed { get; }

            private readonly Func<SO_CharacterStats, CharacterStat> _statGetter;
            private readonly Func<SO_CharacterStats, float> _computedGetter;

            private StatMetric(string section, string label, Func<SO_CharacterStats, CharacterStat> statGetter, Func<SO_CharacterStats, float> computedGetter, bool isComputed)
            {
                Section = section;
                Label = label;
                _statGetter = statGetter;
                _computedGetter = computedGetter;
                IsComputed = isComputed;
            }

            public static StatMetric Raw(string section, string label, Func<SO_CharacterStats, CharacterStat> getter)
            {
                return new StatMetric(section, label, getter, null, false);
            }

            public static StatMetric Computed(string section, string label, Func<SO_CharacterStats, float> getter)
            {
                return new StatMetric(section, label, null, getter, true);
            }

            public CharacterStat GetStat(PlayerCharacter character)
            {
                return _statGetter?.Invoke(character.Stats);
            }

            public float GetValue(PlayerCharacter character)
            {
                if (!character || !character.Stats)
                    return 0f;

                if (IsComputed)
                    return _computedGetter?.Invoke(character.Stats) ?? 0f;

                return GetStat(character)?.Value ?? 0f;
            }
        }

        private static Color GetDeltaColor(float delta)
        {
            return delta switch
            {
                > 0.001f => new Color(0.35f, 1f, 0.45f),
                < -0.001f => new Color(1f, 0.35f, 0.35f),
                _ => new Color(0.65f, 0.65f, 0.65f)
            };
        }

        private static string Format(float value)
        {
            return value.ToString("0.###");
        }

        private static string FormatDelta(float baseValue, float value)
        {
            float delta = value - baseValue;

            if (Mathf.Approximately(delta, 0f))
                return "0";

            if (Mathf.Abs(baseValue) < 0.0001f)
                return delta > 0f ? $"+{Format(delta)}" : Format(delta);

            float percent = delta / baseValue * 100f;
            string deltaPrefix = delta > 0f ? "+" : "";
            string percentPrefix = percent > 0f ? "+" : "";

            return $"{deltaPrefix}{Format(delta)} ({percentPrefix}{percent:0.#}%)";
        }

        private static string FormatModifierValue(StatModifier modifier)
        {
            return modifier.Type switch
            {
                E_StatModType.PercentAdd or E_StatModType.PercentMult => $"{modifier.Value * 100f:+0.#;-0.#;0}%",
                _ => modifier.Value.ToString("+0.###;-0.###;0")
            };
        }
    }
}