using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Net;
using System.Globalization;

namespace MortierFu.Editor
{
    public static class AugmentImporter
    {
        const string CsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vRC-m-ejBnE1EFgk3lWkqC3SiQJ4JCBJtJ4CP873pM1Tpi2KXDDUFVMtWvbcdDFAtUlG4XS57NVdiFz/pub?gid=964454951&single=true&output=csv";
        const string DatabasePath = "Assets/04_Data/Augments/DA_AugmentDatabase.asset"; 

        [MenuItem("Mortier Fu/Import Augments From Sheet")]
        public static void Import()
        {
            string csv;
            try
            {
                csv = new WebClient().DownloadString(CsvUrl);
            }
            catch (Exception e)
            {
                Debug.LogError($"Échec : {e.Message}");
                return;
            }

            List<Dictionary<string, string>> rows = ParseCsv(csv);
            
            ImportStatParams(rows);
            ImportRarities(rows);

            AssetDatabase.SaveAssets();
            Debug.Log("Import terminé.");
        }

        // ------------------------------------------------------------------------------
        // Import des Stats du GSheets 
        static void ImportStatParams(List<Dictionary<string, string>> rows)
        {
            var database = AssetDatabase.LoadAssetAtPath<SO_AugmentDatabase>(DatabasePath);
            
            int success = 0, skipped = 0;

            foreach (var row in rows)
            {
                string name = row.TryGetValue("Name", out var n) ? n : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                string fieldName = name + "Params";
                FieldInfo paramsField = typeof(SO_AugmentDatabase).GetField(fieldName);

                if (paramsField == null)
                {
                    Debug.LogWarning($"[Stats][{name}] '{fieldName}' introuvable dans SO_AugmentDatabase");
                    skipped++;
                    continue;
                }

                object paramsStruct = paramsField.GetValue(database);
                bool modified = ApplyStatChanges(paramsStruct, row, name);
                paramsField.SetValue(database, paramsStruct); 

                if (modified) success++;
            }

            EditorUtility.SetDirty(database);
            Debug.Log($"[Stats] {success} Augments mis à jour, {skipped} ignorés");
        }

        static bool ApplyStatChanges(object paramsObj, Dictionary<string, string> row, string augmentName)
        {
            Type paramsType = paramsObj.GetType();
            bool anyChange = false;

            for (int i = 1; i <= 10; i++)
            {
                string statKey = $"Stat Changed #{i}";
                string exprKey = $"Expression #{i}";
                string valueKey = $"Value #{i}";

                if (!row.ContainsKey(statKey)) break;

                string statName = row[statKey];
                if (string.IsNullOrWhiteSpace(statName)) continue;
                statName = statName.Trim();

                string rawValue = row.TryGetValue(valueKey, out var v) ? v : "0";
                if (!TryParseFloatFR(rawValue, out float value))
                {
                    Debug.LogWarning($"[Stats][{augmentName}] valeur invalide '{rawValue}' pour '{statName}'");
                    continue;
                }

                string modFieldName = statName + "Mod";
                FieldInfo statModField = paramsType.GetField(modFieldName);

                if (statModField != null)
                {
                    // Cherce le ModType pour ajuster
                    string exprRaw = row.TryGetValue(exprKey, out var e) ? e : "Flat";
                    E_StatModType modType = ParseStatModType(exprRaw);
                    statModField.SetValue(paramsObj, new AugmentStatMod { Value = value, ModType = modType });
                    anyChange = true;
                }
                /*
                FieldInfo rawFloatField = paramsType.GetField(
                 */
                else
                {
                    // Cherche s'il y a pas de ModType, une floatt 
                    FieldInfo rawFloatField = paramsType.GetField(statName);
                    if (rawFloatField != null && rawFloatField.FieldType == typeof(float))
                    {
                        rawFloatField.SetValue(paramsObj, value);
                        anyChange = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[Stats][{augmentName}] aucun paramètre trouvé pour '{statName}'");
                    }
                }
            }

            return anyChange;
        }

        static E_StatModType ParseStatModType(string raw)
        {
            switch (raw.Trim().ToLowerInvariant())
            {
                case "flat": return E_StatModType.Flat;
                case "percent add":
                case "percentadd": return E_StatModType.PercentAdd;
                case "percent mult":
                case "percentmult": return E_StatModType.PercentMult;
                default:
                    Debug.LogWarning($"Type d'expression inconnu: '{raw}'");
                    return E_StatModType.Flat;
            }
        }

        // ------------------------------------------------------------------------------
        // Import de la Rareté pour chaque SO_Augment_Name
        static void ImportRarities(List<Dictionary<string, string>> rows)
        {
            string[] guids = AssetDatabase.FindAssets("t:SO_Augment");
            var augmentsByName = new Dictionary<string, SO_Augment>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<SO_Augment>(path);
                if (so != null && !string.IsNullOrWhiteSpace(so.Name))
                    augmentsByName[NormalizeName(so.Name)] = so;
            }

            int updated = 0;

            foreach (var row in rows)
            {
                string name = row.TryGetValue("Name", out var n) ? n : null;
                string rarityRaw = row.TryGetValue("Rarity", out var r) ? r : null;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(rarityRaw)) continue;

                if (!augmentsByName.TryGetValue(NormalizeName(name), out var augment))
                {
                    Debug.LogWarning($"[Rarity] Aucun SO_Augment trouvé avec Name='{name}'.");
                    continue;
                }

                E_AugmentRarity parsedRarity = ParseRarity(rarityRaw);
                if (augment.Rarity != parsedRarity)
                {
                    augment.Rarity = parsedRarity;
                    EditorUtility.SetDirty(augment);
                    updated++;
                }
            }

            Debug.Log($"[Rarity] {updated} SO_Augment mis à jour.");
        }

        static E_AugmentRarity ParseRarity(string raw)
        {
            string clean = raw.Contains(".") ? raw.Split('.')[1].Trim() : raw.Trim();

            switch (clean.ToLowerInvariant())
            {
                case "common": return E_AugmentRarity.Common;
                case "rare": return E_AugmentRarity.Rare;
                case "super rare": return E_AugmentRarity.SuperRare;
                case "epic": return E_AugmentRarity.Epic;
                case "legendary": return E_AugmentRarity.Legendary;
                default:
                    Debug.LogWarning($"[Rarity] Valeur inconnue '{raw}'");
                    return E_AugmentRarity.Common;
            }
        }
        
        static string NormalizeName(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------
        // Parse CSV - Corrige si l'import se fait en FR 
        static bool TryParseFloatFR(string raw, out float value)
        {
            return float.TryParse(raw.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        static List<Dictionary<string, string>> ParseCsv(string csv)
        {
            List<string[]> allRows = ParseCsvFull(csv);
            var result = new List<Dictionary<string, string>>();

            if (allRows.Count < 2) return result;

            string[] headers = allRows[1]; // ligne 2 = vrais headers

            for (int i = 2; i < allRows.Count; i++)
            {
                string[] values = allRows[i];
                if (values.Length == 0 || string.IsNullOrWhiteSpace(values[0])) continue;

                var rowDict = new Dictionary<string, string>();
                for (int c = 0; c < headers.Length && c < values.Length; c++)
                    rowDict[headers[c].Trim()] = values[c].Trim();

                result.Add(rowDict);
            }
            return result;
        }

        static List<string[]> ParseCsvFull(string csv)
        {
            var rows = new List<string[]>();
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"') { current.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else current.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                    else if (c == '\r') { /* ignore */ }
                    else if (c == '\n')
                    {
                        fields.Add(current.ToString());
                        current.Clear();
                        rows.Add(fields.ToArray());
                        fields = new List<string>();
                    }
                    else current.Append(c);
                }
            }

            if (current.Length > 0 || fields.Count > 0)
            {
                fields.Add(current.ToString());
                rows.Add(fields.ToArray());
            }

            return rows;
        }
    }
}