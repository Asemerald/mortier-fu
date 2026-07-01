using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MortierFu.Editor
{
    public sealed class AugmentDebugTab : IDisposable
    {
        private const double k_refreshInterval = 0.25d;

        private readonly VisualElement _root;
        private readonly List<SO_Augment> _augments = new();
        private readonly List<PlayerCharacter> _players = new();

        private VisualElement _playerContainer;
        private VisualElement _selectedBuildContainer;
        private VisualElement _augmentContainer;
        private Label _statusLabel;
        private TextField _searchField;

        private PlayerCharacter _selectedPlayer;
        private string _searchText = string.Empty;

        private double _nextRefreshTime;
        private bool _isSubscribedToEditorUpdate;

        public AugmentDebugTab(VisualElement root)
        {
            _root = root;
        }

        public void Show()
        {
            _root.Clear();
            _root.style.flexGrow = 1f;
            _root.style.minHeight = 0;

            LoadAugments();
            BuildLayout();
            RefreshRuntimeView(forceRebuild: true);

            if (_isSubscribedToEditorUpdate)
                return;

            EditorApplication.update += EditorUpdate;
            _isSubscribedToEditorUpdate = true;
        }

        public void Dispose()
        {
            if (!_isSubscribedToEditorUpdate)
                return;

            EditorApplication.update -= EditorUpdate;
            _isSubscribedToEditorUpdate = false;
        }

        private void EditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextRefreshTime)
                return;

            _nextRefreshTime = EditorApplication.timeSinceStartup + k_refreshInterval;
            RefreshRuntimeView(forceRebuild: false);
        }

        private void BuildLayout()
        {
            BuildHeader();

            VisualElement body = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1f,
                    minHeight = 0
                }
            };

            VisualElement leftPanel = BuildLeftPanel();
            VisualElement rightPanel = BuildRightPanel();

            body.Add(leftPanel);
            body.Add(rightPanel);

            _root.Add(body);
        }

        private void BuildHeader()
        {
            Label title = new("Augment Injector")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 18,
                    marginBottom = 4
                }
            };

            Label subtitle = new("Inject augments directly into runtime players without going through the race phase.")
            {
                style =
                {
                    color = new Color(0.7f, 0.7f, 0.7f),
                    marginBottom = 10
                }
            };

            _statusLabel = new Label
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 8
                }
            };

            _root.Add(title);
            _root.Add(subtitle);
            _root.Add(_statusLabel);
        }

        private VisualElement BuildLeftPanel()
        {
            VisualElement panel = new VisualElement
            {
                style =
                {
                    width = 330,
                    minWidth = 330,
                    maxWidth = 330,
                    flexShrink = 0f,
                    marginRight = 12,
                    paddingRight = 8,
                    borderRightWidth = 1,
                    borderRightColor = new Color(0.25f, 0.25f, 0.25f)
                }
            };

            panel.Add(MakeSectionTitle("Runtime Players"));

            Button refreshPlayersButton = new(() => RefreshRuntimeView(forceRebuild: true))
            {
                text = "Refresh Players",
                style =
                {
                    marginBottom = 8
                }
            };
            panel.Add(refreshPlayersButton);

            ScrollView playerScroll = new (ScrollViewMode.Vertical)
            {
                style =
                {
                    height = 180,
                    minHeight = 180,
                    maxHeight = 180,
                    marginBottom = 10
                }
            };

            _playerContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 0f,
                    flexShrink = 0f
                }
            };

            playerScroll.Add(_playerContainer);
            panel.Add(playerScroll);

            panel.Add(MakeSectionTitle("Selected Build"));

            Button clearButton = new(ClearSelectedPlayerAugments)
            {
                text = "Clear Selected Player Augments",
                style =
                {
                    marginBottom = 8
                }
            };
            panel.Add(clearButton);

            ScrollView buildScroll = new(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1f,
                    minHeight = 0
                }
            };

            _selectedBuildContainer = new VisualElement();
            buildScroll.Add(_selectedBuildContainer);
            panel.Add(buildScroll);

            return panel;
        }

        private VisualElement BuildRightPanel()
        {
            VisualElement panel = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    minWidth = 0,
                    minHeight = 0
                }
            };

            VisualElement toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 8
                }
            };

            _searchField = new TextField
            {
                value = _searchText,
                style =
                {
                    flexGrow = 1f,
                    marginRight = 8
                }
            };
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue ?? string.Empty;
                RebuildAugmentList();
            });

            Button refreshButton = new(() =>
            {
                LoadAugments();
                RebuildAugmentList();
            })
            {
                text = "Refresh Augments"
            };

            toolbar.Add(_searchField);
            toolbar.Add(refreshButton);

            panel.Add(MakeSectionTitle("Available Augments"));
            panel.Add(toolbar);

            ScrollView augmentScroll = new(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1f,
                    minHeight = 0
                }
            };

            _augmentContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 0f,
                    flexShrink = 0f
                }
            };

            augmentScroll.Add(_augmentContainer);
            panel.Add(augmentScroll);

            RebuildAugmentList();

            return panel;
        }

        private void RefreshRuntimeView(bool forceRebuild)
        {
            if (!Application.isPlaying)
            {
                _statusLabel.text = "Enter Play Mode to inject augments.";
                _statusLabel.style.color = new Color(1f, 0.75f, 0.25f);

                _players.Clear();
                _selectedPlayer = null;

                RebuildPlayerList();
                RebuildSelectedBuild();
                return;
            }

            _statusLabel.text = "Play Mode active. Select a player, then inject an augment.";
            _statusLabel.style.color = new Color(0.35f, 1f, 0.45f);

            int previousCount = _players.Count;
            CollectPlayers();

            if (!_selectedPlayer || !_players.Contains(_selectedPlayer))
                _selectedPlayer = _players.Count > 0 ? _players[0] : null;

            if (forceRebuild || previousCount != _players.Count)
                RebuildPlayerList();

            RebuildSelectedBuild();
        }

        private void CollectPlayers()
        {
            _players.Clear();

            LobbyService lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            if (lobbyService != null)
            {
                var managers = lobbyService.GetPlayers();

                for (int i = 0; i < managers.Count; i++)
                {
                    PlayerManager manager = managers[i];

                    if (!manager || !manager.Character)
                        continue;

                    if (!_players.Contains(manager.Character))
                        _players.Add(manager.Character);
                }
            }

            var characters = UnityEngine.Object.FindObjectsByType<PlayerCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < characters.Length; i++)
            {
                PlayerCharacter character = characters[i];

                if (!character)
                    continue;

                if (!_players.Contains(character))
                    _players.Add(character);
            }

            _players.Sort((a, b) =>
            {
                int aIndex = a.Owner ? a.Owner.PlayerIndex : int.MaxValue;
                int bIndex = b.Owner ? b.Owner.PlayerIndex : int.MaxValue;
                return aIndex.CompareTo(bIndex);
            });
        }

        private void RebuildPlayerList()
        {
            if (_playerContainer == null)
                return;

            _playerContainer.Clear();

            if (!Application.isPlaying)
            {
                _playerContainer.Add(MakeInfoLabel("No runtime players. Enter Play Mode."));
                return;
            }

            if (_players.Count == 0)
            {
                _playerContainer.Add(MakeInfoLabel("No PlayerCharacter found."));
                return;
            }

            for (int i = 0; i < _players.Count; i++)
            {
                _playerContainer.Add(MakePlayerButton(_players[i]));
            }
        }

        private VisualElement MakePlayerButton(PlayerCharacter player)
        {
            int playerIndex = player.Owner ? player.Owner.PlayerIndex + 1 : 0;
            bool selected = player == _selectedPlayer;

            Button button = new(() =>
            {
                _selectedPlayer = player;
                RebuildPlayerList();
                RebuildSelectedBuild();
            })
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Stretch,
                    marginBottom = 6,
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = selected
                        ? new Color(0.22f, 0.34f, 0.55f)
                        : new Color(0.16f, 0.16f, 0.16f)
                }
            };

            Label title = new(playerIndex > 0 ? $"Player {playerIndex}" : "Player ?")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white
                }
            };

            Label subtitle = new($"{player.name}  •  Augments: {player.Augments.Count}")
            {
                style =
                {
                    color = new Color(0.75f, 0.75f, 0.75f),
                    fontSize = 11
                }
            };

            button.Add(title);
            button.Add(subtitle);

            return button;
        }

        private void RebuildSelectedBuild()
        {
            if (_selectedBuildContainer == null)
                return;

            _selectedBuildContainer.Clear();

            if (!_selectedPlayer)
            {
                _selectedBuildContainer.Add(MakeInfoLabel("No player selected."));
                return;
            }

            int playerIndex = _selectedPlayer.Owner ? _selectedPlayer.Owner.PlayerIndex + 1 : 0;

            _selectedBuildContainer.Add(new Label(playerIndex > 0 ? $"Player {playerIndex} Build" : "Selected Player Build")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 6,
                    color = Color.white
                }
            });

            if (_selectedPlayer.Augments.Count == 0)
            {
                _selectedBuildContainer.Add(MakeInfoLabel("No active augments."));
                return;
            }

            for (int i = 0; i < _selectedPlayer.Augments.Count; i++)
            {
                IAugment augmentInstance = _selectedPlayer.Augments[i];

                _selectedBuildContainer.Add(new Label($"• {augmentInstance.GetType().Name}")
                {
                    style =
                    {
                        color = new Color(0.78f, 0.9f, 1f),
                        marginBottom = 3
                    }
                });
            }
        }

        private void LoadAugments()
        {
            _augments.Clear();

            string[] guids = AssetDatabase.FindAssets("t:SO_Augment");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var augment = AssetDatabase.LoadAssetAtPath<SO_Augment>(path);

                if (!augment)
                    continue;

                if (!_augments.Contains(augment))
                    _augments.Add(augment);
            }

            _augments.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        }

        private void RebuildAugmentList()
        {
            if (_augmentContainer == null)
                return;

            _augmentContainer.Clear();

            int visibleCount = 0;

            for (int i = 0; i < _augments.Count; i++)
            {
                SO_Augment augment = _augments[i];

                if (!MatchesSearch(augment))
                    continue;

                _augmentContainer.Add(MakeAugmentCard(augment));
                visibleCount++;
            }

            if (visibleCount == 0)
                _augmentContainer.Add(MakeInfoLabel("No augment found."));
        }

        private bool MatchesSearch(SO_Augment augment)
        {
            if (!augment)
                return false;

            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            return augment.name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private VisualElement MakeAugmentCard(SO_Augment augment)
        {
            VisualElement card = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexShrink = 0f,
                    minHeight = 44,
                    marginBottom = 6,
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = new Color(0.13f, 0.13f, 0.13f),
                    borderBottomWidth = 1,
                    borderTopWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderBottomColor = new Color(0.25f, 0.25f, 0.25f),
                    borderTopColor = new Color(0.25f, 0.25f, 0.25f),
                    borderLeftColor = new Color(0.25f, 0.25f, 0.25f),
                    borderRightColor = new Color(0.25f, 0.25f, 0.25f)
                }
            };

            VisualElement info = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    minWidth = 0
                }
            };

            Label title = new Label(augment.name)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white
                }
            };

            string path = AssetDatabase.GetAssetPath(augment);
            Label subtitle = new(path)
            {
                style =
                {
                    color = new Color(0.55f, 0.55f, 0.55f),
                    fontSize = 10
                }
            };

            info.Add(title);
            info.Add(subtitle);

            Button addButton = new(() => AddAugmentToSelectedPlayer(augment))
            {
                text = "Add",
                style =
                {
                    width = 70,
                    marginLeft = 8
                }
            };

            Button addAllButton = new(() => AddAugmentToAllPlayers(augment))
            {
                text = "Add All",
                style =
                {
                    width = 80,
                    marginLeft = 6
                }
            };

            Button pingButton = new(() =>
            {
                EditorGUIUtility.PingObject(augment);
                Selection.activeObject = augment;
            })
            {
                text = "Ping",
                style =
                {
                    width = 60,
                    marginLeft = 6
                }
            };

            card.Add(info);
            card.Add(addButton);
            card.Add(addAllButton);
            card.Add(pingButton);

            return card;
        }

        private void AddAugmentToSelectedPlayer(SO_Augment augment)
        {
            if (!CanInjectAugment(augment, _selectedPlayer))
                return;

            InjectAugment(_selectedPlayer, augment);
            RefreshRuntimeView(forceRebuild: true);
        }

        private void AddAugmentToAllPlayers(SO_Augment augment)
        {
            if (!Application.isPlaying)
            {
                Logs.LogWarning("[AugmentDebugTab] Cannot inject augment outside Play Mode.");
                return;
            }

            CollectPlayers();

            for (int i = 0; i < _players.Count; i++)
            {
                if (CanInjectAugment(augment, _players[i], logErrors: false))
                    InjectAugment(_players[i], augment);
            }

            RefreshRuntimeView(forceRebuild: true);
        }

        private bool CanInjectAugment(SO_Augment augment, PlayerCharacter player, bool logErrors = true)
        {
            if (!Application.isPlaying)
            {
                if (logErrors)
                    Logs.LogWarning("[AugmentDebugTab] Cannot inject augment outside Play Mode.");

                return false;
            }

            if (!augment)
            {
                if (logErrors)
                    Logs.LogWarning("[AugmentDebugTab] Cannot inject a null augment.");

                return false;
            }

            if (!player)
            {
                if (logErrors)
                    Logs.LogWarning("[AugmentDebugTab] No player selected.");

                return false;
            }

            if (SystemManager.Instance == null || SystemManager.Config == null || !SystemManager.Config.AugmentDatabase)
            {
                if (logErrors)
                    Logs.LogError("[AugmentDebugTab] SystemManager.Config.AugmentDatabase is missing. Cannot create augment.");

                return false;
            }

            return true;
        }

        private void InjectAugment(PlayerCharacter player, SO_Augment augment)
        {
            try
            {
                player.AddAugment(augment);

                int playerIndex = player.Owner ? player.Owner.PlayerIndex + 1 : 0;

                Logs.Log(
                    $"[AugmentDebugTab] Injected augment '{augment.name}' into Player {playerIndex}.",
                    player
                );
            }
            catch (Exception e)
            {
                Logs.LogError($"[AugmentDebugTab] Failed to inject augment '{augment.name}': {e}");
            }
        }

        private void ClearSelectedPlayerAugments()
        {
            if (!_selectedPlayer)
            {
                Logs.LogWarning("[AugmentDebugTab] No player selected.");
                return;
            }

            _selectedPlayer.ClearAugments();

            int playerIndex = _selectedPlayer.Owner ? _selectedPlayer.Owner.PlayerIndex + 1 : 0;

            Logs.Log($"[AugmentDebugTab] Cleared all augments from Player {playerIndex}.", _selectedPlayer);

            RefreshRuntimeView(forceRebuild: true);
        }

        private static Label MakeSectionTitle(string title)
        {
            return new Label(title)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginBottom = 6,
                    color = new Color(0.85f, 0.85f, 0.85f)
                }
            };
        }

        private static Label MakeInfoLabel(string text)
        {
            return new Label(text)
            {
                style =
                {
                    color = new Color(0.65f, 0.65f, 0.65f),
                    unityFontStyleAndWeight = FontStyle.Italic,
                    marginBottom = 4
                }
            };
        }
    }
}