﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Nuve.Lang;
using Nuve.Morphologic;
using Nuve.Morphologic.Structure;
using Nuve.Orthographic;
using Nuve.Properties;

namespace Nuve.Reader
{
    public class LanguageReader
    {
        private const string Delimiter = "\t";

        public const string DefaultTableName = "sheet";
        private readonly string _dirPath;
        private readonly bool _external;
        private readonly string _seperator;
        private Orthography _orthography;

        private readonly TraceSource _trace = new TraceSource("LanguageReader");

        public LanguageReader(string dirPath) : this(dirPath, true)
        {
        }

        internal LanguageReader(string langCode, bool external)
        {
            _dirPath = langCode;
            _external = external;
            _seperator = external ? "/" : ".";
        }

        public Language Read()
        {
            var sw = new Stopwatch();
            sw.Start();

            _orthography = ReadOrthography();

            _trace.TraceInformation($"{_dirPath} orthography loading time is {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var morphotactics = ReadMorphotactics();
            _trace.TraceInformation($"{_dirPath} morphotactics loading time is {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var roots = ReadRoots();
            _trace.TraceInformation($"{_dirPath} roots loading time is {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var suffixes = ReadSuffixes();
            _trace.TraceInformation($"{_dirPath} suffixes loading time is {sw.ElapsedMilliseconds} ms");
            sw.Restart();


            var index = _dirPath.LastIndexOf("\\", StringComparison.Ordinal);

            var langCode = index > -1 ? _dirPath.Substring(index + 1) : _dirPath;

            return new Language(langCode, morphotactics, roots, suffixes);
        }

        private Orthography ReadOrthography()
        {
            try
            {
                var path = _dirPath + _seperator + Resources.OrthographyFileName;

                var xml = new XmlDocument();
                var reader = GetXmlReader(path);
                xml.Load(reader);

                return OrthographyReader.Read(xml);
            }

            catch (Exception ex)
            {
                throw new InvalidLanguageFileException(ex, Type.Orthograpy, "Invalid language file for orthograpy: ");
            }
        }

        private Morphotactics ReadMorphotactics()
        {
            try
            {
                var path = _dirPath + _seperator + Resources.MorphotacticsFileName;

                if (_external)
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        return MorphotacticsReader.Read(stream, _orthography.Alphabet);
                    }
                }

                var xml = EmbeddedResourceReader.Read(path);
                return MorphotacticsReader.Read(xml, _orthography.Alphabet);
            }
            catch (Exception ex)
            {
                throw new InvalidLanguageFileException(ex, Type.Morphotactics,
                    "Invalid language file for Morphotactics: ");
            }
        }

        private MorphemeContainer<Root> ReadRoots()
        {
            try
            {
                var rootsBySurface = new MorphemeSurfaceDictionary<Root>();
                var rootsById = new Dictionary<string, Root>();
                var reader = new RootLexiconReader(_orthography);

                var rootsPath = _dirPath + _seperator + Resources.InternalMainRootsPath;

                if (_external)
                {
                    using (var stream = new FileStream(rootsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var ds = TextToDataSet.Convert(stream, DefaultTableName, Delimiter);
                        reader.AddEntries(ds, DefaultTableName, rootsById, rootsBySurface);
                    }

                    return new MorphemeContainer<Root>(rootsById, rootsBySurface);
                }

                reader.AddEntries(EmbeddedTextResourceToDataSet(rootsPath), DefaultTableName, rootsById, rootsBySurface);

                var namesPath = _dirPath + _seperator + Resources.InternalPersonNamesPath;
                reader.AddEntries(EmbeddedTextResourceToDataSet(namesPath), DefaultTableName, rootsById, rootsBySurface);

                var abbreviationPath = _dirPath + _seperator + Resources.InternalAbbreviationsPath;
                reader.AddEntries(EmbeddedTextResourceToDataSet(abbreviationPath), DefaultTableName, rootsById, rootsBySurface);

                return new MorphemeContainer<Root>(rootsById, rootsBySurface);
            }
            catch (Exception ex)
            {
                throw new InvalidLanguageFileException(ex, Type.Roots, "Invalid language file for roots: ");
            }
        }

        private MorphemeContainer<Suffix> ReadSuffixes()
        {
            try
            {
                var path = _dirPath + _seperator + Resources.InternalSuffixesPath;
                var reader = new SuffixLexiconReader(_orthography);

                if (_external)
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        return reader.Read(TextToDataSet.Convert(stream, DefaultTableName, Delimiter), DefaultTableName);
                    }
                }

                var dataSet = EmbeddedTextResourceToDataSet(path);

                return reader.Read(dataSet, DefaultTableName);
            }
            catch (Exception ex)
            {
                throw new InvalidLanguageFileException(ex, Type.Suffixes, "Invalid language file for suffixes: ");
            }
        }


        private DataSet EmbeddedTextResourceToDataSet(string path)
        {
            var textStream = EmbeddedResourceReader.Read(path);
            return TextToDataSet.Convert(textStream, DefaultTableName, Delimiter);
        }


        private XmlReader GetXmlReader(string path)
        {
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.DTD,
                DtdProcessing = DtdProcessing.Parse
            };
            settings.ValidationEventHandler += OnValidationEvent;

            XmlReader reader;
            if (!_external)
            {
                settings.XmlResolver = new EmbeddedXmlResolver();
                var stream = EmbeddedResourceReader.Read(path);
                reader = XmlReader.Create(stream, settings);
            }

            else
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                settings.XmlResolver = new ExternalDtdUrlResolver(path);
                reader = XmlReader.Create(stream, settings);
            }

            return reader;
        }

        private void OnValidationEvent(object o, ValidationEventArgs args)
        {
            throw new XmlException("Invalid Orthography XML: " + args.Message, args.Exception);
        }
    }
}