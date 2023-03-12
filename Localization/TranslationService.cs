﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;


// Contributed by Apache#8842 over discord 20th of October 2021. Edited by Tyron
// Apache — Today at 11:45 AM
// If you want to use it, it's under your license... I made it to be added to the game. ITranslationService is so that it can be mocked within your Test Suite, added to an IOC Container, extended via mods, etc. Interfaces should be used, in favour of concrete classes, in  most cases. Especially where you have volatile code such as IO.


namespace Vintagestory.API.Config
{
    /// <summary>
    /// A service, which provides access to translated strings, based on key/value pairs read from JSON files.
    /// </summary>
    /// <seealso cref="ITranslationService" />
    public class TranslationService : ITranslationService
    {
        private Dictionary<string, string> entryCache = new Dictionary<string, string>();
        private Dictionary<string, KeyValuePair<Regex, string>> regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();
        private Dictionary<string, string> wildcardCache = new Dictionary<string, string>();
        private HashSet<string> notFound = new HashSet<string>();

        private IAssetManager assetManager;
        private readonly ILogger logger;
        private bool loaded = false;
        private string preLoadAssetsPath = null;
        public EnumLinebreakBehavior LineBreakBehavior { get; set; }


        /// <summary>
        /// Initialises a new instance of the <see cref="TranslationService" /> class.
        /// </summary>
        /// <param name="languageCode">The language code that this translation service caters for.</param>
        /// <param name="logger">The <see cref="ILogger" /> instance used within the sided API.</param>
        /// <param name="assetManager">The <see cref="IAssetManager" /> instance used within the sided API.</param>
        public TranslationService(string languageCode, ILogger logger, IAssetManager assetManager = null, EnumLinebreakBehavior lbBehavior = EnumLinebreakBehavior.AfterWord)
        {
            LanguageCode = languageCode;
            this.logger = logger;
            this.assetManager = assetManager;
            this.LineBreakBehavior = lbBehavior;
        }

        /// <summary>
        /// Gets the language code that this translation service caters for.
        /// </summary>
        /// <value>A string, that contains the language code that this translation service caters for.</value>
        public string LanguageCode { get; }

        /// <summary>
        /// Loads translation key/value pairs from all relevant JSON files within the Asset Manager.
        /// </summary>
        public void Load(bool lazyLoad = false)
        {
            preLoadAssetsPath = null;
            if (lazyLoad) return;
            loaded = true;

            // Don't work on dicts directly for thread safety (client and local server access the same dict)
            var entryCache = new Dictionary<string, string>();
            var regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();
            var wildcardCache = new Dictionary<string, string>();

            var origins = assetManager.Origins;

            foreach (var asset in origins.SelectMany(p => p.GetAssets(AssetCategory.lang).Where(a => a.Name.Equals($"{LanguageCode}.json"))))
            {

                try
                {
                    var json = asset.ToText();
                    LoadEntries(entryCache, regexCache, wildcardCache, JsonConvert.DeserializeObject<Dictionary<string, string>>(json), asset.Location.Domain);
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to load language file: {asset.Name} \n\n\t {ex}");
                }
            }

            this.entryCache = entryCache;
            this.regexCache = regexCache;
            this.wildcardCache = wildcardCache;
        }

        /// <summary>
        /// Loads only the vanilla JSON files, without dealing with mods, or resource-packs.
        /// </summary>
        /// <param name="assetsPath">The root assets path to load the vanilla files from.</param>
        public void PreLoad(string assetsPath, bool lazyLoad = false)
        {
            preLoadAssetsPath = assetsPath;
            if (lazyLoad) return;
            
            loaded = true;

            // Don't work on dicts directly for thread safety (client and local server access the same dict)
            var entryCache = new Dictionary<string, string>();
            var regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();
            var wildcardCache = new Dictionary<string, string>();


            var assetsDirectory = new DirectoryInfo(Path.Combine(assetsPath, GlobalConstants.DefaultDomain, "lang"));
            var files = assetsDirectory.EnumerateFiles($"{LanguageCode}.json", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file.FullName);
                    LoadEntries(entryCache, regexCache, wildcardCache, JsonConvert.DeserializeObject<Dictionary<string, string>>(json));
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to load language file: {file.Name} \n\n\t {ex}");
                }
            }

            this.entryCache = entryCache;
            this.regexCache = regexCache;
            this.wildcardCache = wildcardCache;
            
        }

        protected void EnsureLoaded()
        {
            if (!loaded)
            {
                if (preLoadAssetsPath != null) PreLoad(preLoadAssetsPath);
                else Load();
            }
        }

        /// <summary>
        /// Sets the loaded flag to false, so that the next lookup causes it to reload all translation entries
        /// </summary>
        public void Invalidate()
        {
            loaded = false;
        }

        protected string Format(string value, params object[] args)
        {
            if (value.ContainsFast("{p")) return PluralFormat(value, args);
            return string.Format(value, args);
        }

        // General format: {p#:string0|string1|string2...} where # is a parameter index similar to using {0}, so zero for the first parameter etc.  That parameter should be a number, N
        // The strings string0, string1, string2 etc. will be the actual desired output for different values of N.
        // Most languages have different grammar rules for writing different numbers of objects, e.g. zero, one, two, more than two
        // These strings are separated by | with no spaces (any spaces you type will be in the output)
        // Left to right, these will be the language strings for N=0, N=1, N=2, N=3, N=4 etc.  The last one given continues to be repeated for all higher N.
        // The number N can be itself output in the string using a standard number format, for example #.00 
        //
        // Examples:
        // {p3:fish}                                                                   args[3] is N, output for different N is:  0+: fish
        // {p0:no apples|# apple|# apples}                                             args[0] is N, output for different N is:  0: no apples, 1: 1 apple, 2: 2 apples, 3: 3 apples, ... etc
        // {p9:no cake|a cake|a couple of cakes|a few cakes|a few cakes|many cakes}    args[9] is N, output for different N is:  0: no cake, 1: a cake, 2: a couple of cakes, 3-4: a few cakes, 5+: many cakes
        //
        private string PluralFormat(string value, object[] args)
        {
            int start = value.IndexOf("{p");
            if (value.Length < start + 5) return string.Format(value, args);   // Fail: too short to even allow sense checks without error
            int pluralOffset = start + 4;
            int end = value.IndexOf("}", pluralOffset);

            // Sense checks
            char c = value[start + 2];
            if (c < '0' || c > '9') return string.Format(value, args);   // Fail: no argument number specified
            if (end < 0) return string.Format(value, args);   // Fail: no closing curly brace
            int argNum = c - '0';
            if ((c = value[start + 3]) != ':')
            {
                if (value[start + 4] == ':' && c >= '0' && c <= '9')
                {
                    argNum = argNum * 10 + c - '0';
                    pluralOffset++;
                }
                else return string.Format(value, args);   // Fail: no colon in position 3 or 4
            }
            if (argNum >= args.Length) throw new IndexOutOfRangeException("Index out of range: Plural format {p#:...} referenced an argument " + argNum + " but only " + args.Length + " arguments were available in the code");
            float N = 0;
            try
            {
                N = float.Parse(args[argNum].ToString());
            }
            catch (Exception _) { }

            // Separate out the different elements of this string

            string before = value.Substring(0, start);
            string plural = value.Substring(pluralOffset, end - pluralOffset);
            string after = value.Substring(end + 1);

            object[] argsBefore = new object[argNum];
            for (int i = 0; i < argNum; i++) argsBefore[i] = args[i];

            object[] argsAfter = new object[args.Length - argNum - 1];
            for (int i = argNum + 1; i < args.Length; i++) argsAfter[i - argNum - 1] = args[i];

            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(before, argsBefore));
            sb.Append(BuildPluralFormat(plural, N));
            sb.Append(Format(after, argsAfter));   // there could be further instances of {p#:...} after this
            return sb.ToString();
        }

        private string BuildPluralFormat(string input, float n)
        {
            int index = (int)Math.Ceiling(n);   // this implments a rule: 0 -> 0;  0.5 -> 1;  1 -> 1;  1.5 -> 2  etc.   This may not be appropriate for all languages e.g. French.  A future extension can allow more customisation by specifying math formulae
            string[] plurals = input.Split('|');
            if (index < 0 || index >= plurals.Length) index = plurals.Length - 1;

            string rawResult = plurals[index];
            return WithNumberFormatting(rawResult, n);
        }

        private string WithNumberFormatting(string rawResult, float n)
        {
            int j = rawResult.IndexOf('#');
            if (j < 0) return rawResult;

            string partA = rawResult.Substring(0, j);
            int k = j;
            while (++k < rawResult.Length)
            {
                char c = rawResult[k];
                if (c != '#' && c != '.' && c != '0' && c != ',') break;
            }
            string numberFormatting = rawResult.Substring(j, k - j);
            string partB = rawResult.Substring(k);

            string number;
            try
            {
                number = n.ToString(numberFormatting);
            }
            catch (Exception _) { number = n.ToString(); }      // Fallback if the translators gave us a badly formatted number string

            return partA + number + WithNumberFormatting(partB, n);
        }

        /// <summary>
        /// Gets a translation for a given key, if any matching wildcarded keys are found within the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="args">The arguments to interpolate into the resulting string.</param>
        /// <returns>
        ///     Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated
        ///     value.
        /// </returns>
        public string GetIfExists(string key, params object[] args)
        {
            EnsureLoaded();
            return entryCache.TryGetValue(KeyWithDomain(key), out var value)
                ? Format(value, args)
                : null;
        }

        /// <summary>
        /// Gets a translation for a given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="args">The arguments to interpolate into the resulting string.</param>
        /// <returns>
        ///     Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated
        ///     value.
        /// </returns>
        public string Get(string key, params object[] args)
        {
              return Format(GetUnformatted(key), args);   // There will be a cacheLock and EnsureLoaded inside the called method GetUnformatted
        }

        /// <summary>
        /// Retrieves a list of all translation entries within the cache.
        /// </summary>
        /// <returns>A dictionary of localisation entries.</returns>
        public IDictionary<string, string> GetAllEntries()
        {
            EnsureLoaded();
            return entryCache;
        }

        /// <summary>
        /// Gets the raw, unformatted translated value for the key provided.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///     Returns the key as a default value, if no results are found; otherwise returns the unformatted, translated
        ///     value.
        /// </returns>
        public string GetUnformatted(string key)
        {
            EnsureLoaded();
            bool found = entryCache.TryGetValue(KeyWithDomain(key), out var value);
            return found ? value : key;
        }

        /// <summary>
        /// Gets a translation for a given key, if any matching wildcarded keys are found within the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="args">The arguments to interpolate into the resulting string.</param>
        /// <returns>
        /// Returns the key as a default value, if no results are found; otherwise returns the pre-formatted, translated
        /// value.
        /// </returns>
        public string GetMatching(string key, params object[] args)
        {
            EnsureLoaded();
            var value = GetMatchingIfExists(KeyWithDomain(key), args);
            return string.IsNullOrEmpty(value)
                ? Format(key, args)
                : value;
        }

        /// <summary>
        /// Determines whether the specified key has a translation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="findWildcarded">if set to <c>true</c>, the scan will include any wildcarded values.</param>
        /// <returns><c>true</c> if the specified key has a translation; otherwise, <c>false</c>.</returns>
        public bool HasTranslation(string key, bool findWildcarded = true)
        {
            EnsureLoaded();
            var validKey = KeyWithDomain(key);
            if (entryCache.ContainsKey(validKey)) return true;
            if (findWildcarded)
            {
                bool result = wildcardCache.Any(pair => key.StartsWithFast(pair.Key));
                if (!result) result = regexCache.Values.Any(pair => pair.Key.IsMatch(validKey));
                if (!result && !key.Contains("desc-") && notFound.Add(key)) logger.VerboseDebug("Lang key not found: " + key.Replace("{", "{{").Replace("}", "}}"));
                return result;
            }
            return false;
        }

        /// <summary>
        /// Specifies an asset manager to use, when the service has been lazy-loaded.
        /// </summary>
        /// <param name="assetManager">The <see cref="IAssetManager" /> instance used within the sided API.</param>
        public void UseAssetManager(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        /// <summary>
        /// Gets a translation for a given key, if any matching wildcarded keys are found within the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="args">The arguments to interpolate into the resulting string.</param>
        /// <returns>
        /// Returns <c>null</c> as a default value, if no results are found; otherwise returns the pre-formatted, translated value.
        /// </returns>
        public string GetMatchingIfExists(string key, params object[] args)
        {
            EnsureLoaded();
            var validKey = KeyWithDomain(key);

            if (entryCache.TryGetValue(validKey, out var value)) return Format(value, args);

            foreach (var pair in wildcardCache
                .Where(pair => validKey.StartsWithFast(pair.Key)))
                return Format(pair.Value, args);

            return regexCache.Values
                .Where(pair => pair.Key.IsMatch(validKey))
                .Select(pair => Format(pair.Value, args))
                .FirstOrDefault();
        }

        private void LoadEntries(Dictionary<string, string> entryCache, Dictionary<string, KeyValuePair<Regex, string>> regexCache, Dictionary<string, string> wildcardCache, Dictionary<string, string> entries, string domain = GlobalConstants.DefaultDomain)
        {
            foreach (var entry in entries)
            {
                LoadEntry(entryCache, regexCache, wildcardCache, entry, domain);
            }
        }

        private void LoadEntry(Dictionary<string, string> entryCache, Dictionary<string, KeyValuePair<Regex, string>> regexCache, Dictionary<string, string> wildcardCache, KeyValuePair<string, string> entry, string domain = GlobalConstants.DefaultDomain)
        {
            var key = KeyWithDomain(entry.Key, domain);
            switch (key.CountChars('*'))
            {
                case 0:
                    entryCache[key] = entry.Value;
                    break;
                case 1 when key.EndsWith("*"):
                    wildcardCache[key.TrimEnd('*')] = entry.Value;
                    break;
                    // we can probably do better here, as we have our own wildcardsearch now
                default:
                {
                    var regex = new Regex("^" + key.Replace("*", "(.*)") + "$", RegexOptions.Compiled);
                    regexCache[key] = new KeyValuePair<Regex, string>(regex, entry.Value);
                    break;
                }
            }
        }

        private static string KeyWithDomain(string key, string domain = GlobalConstants.DefaultDomain)
        {
            if (key.Contains(AssetLocation.LocationSeparator)) return key;
            return new StringBuilder(domain)
                .Append(AssetLocation.LocationSeparator)
                .Append(key)
                .ToString();
        }

        public void InitialiseSearch()
        {
            regexCache.Values.Any(pair => pair.Key.IsMatch("nonsense_value_and_fairly_longgg"));   // Force compilation of all the regexCache keys on first use
        }
    }
}