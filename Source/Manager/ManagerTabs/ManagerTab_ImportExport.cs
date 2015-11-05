using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using RimWorld;
using Verse;
using System.Reflection;

namespace FM
{
    class ManagerTab_ImportExport : ManagerTab
    {
        private float _margin = Manager.Margin;
        private float _loadAreaRatio = .6f;
        private string _folder = "";
        private List<Pair<string, int>> _jobCounts;
        private string _saveName = "";
        private string _saveNameBase = "ManagerSave_";
        private string _saveExtension = ".rwm";
        private JobStack _jobStackIO;
        private List<SaveFileInfo> _saveFiles;

        public override void PreOpen()
        {
            // set save location
            _folder = GetSaveLocation();

            // variable stuff
            Refresh();
        }

        public void Refresh()
        {
            // List of current job counts
            _jobCounts = ( from job in Manager.Get.JobStack.FullStack
                           group job by job.Tab.Label into jobs
                           select new Pair<string, int>( jobs.Key, jobs.Count() ) ).ToList();

            // fetch the list of saved jobs
            _saveFiles = GetSavedFilesList();

            // set a valid default name
            _saveName = DefaultSaveName();
        }

        private List<SaveFileInfo> GetSavedFilesList()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(_folder);

            // raw files
            var files = from f in directoryInfo.GetFiles()
                        where f.Extension == _saveExtension
                        orderby f.LastWriteTime descending
                        select f;


            // TODO: remove debug section.
            var names = files.Select(f => f.Name);
            foreach (string name in names )
            {
                Log.Message( name );
            }

            // convert to RW save files - mostly for the headers
            List < SaveFileInfo > saves = new List<SaveFileInfo>();
            foreach( FileInfo current in files )
            {
                try
                {
                    saves.Add( new SaveFileInfo( current ) );
                }
                catch( Exception ex )
                {
                    Log.Error( "Exception loading " + current.Name + ": " + ex.ToString() );
                }
            }

            return saves;
        }

        private string GetSaveLocation()
        {
            // Get method "FolderUnderSaveData" from GenFilePaths, which is private (NonPublic) and static.
            var Folder = typeof(GenFilePaths).GetMethod( "FolderUnderSaveData",
                                                                           BindingFlags.NonPublic |
                                                                           BindingFlags.Static );
            if( Folder == null )
            {
                throw new Exception( "FolderUnderSaveData [reflection] is null" );
            }

            // Call "FolderUnderSaveData" from null parameter, since this is a static method.
            return (string)Folder.Invoke( null, new object[] { "ManagerJobs" } );

        }

        private string DefaultSaveName()
        {
            // TODO: actually check saves and find a new one rather than rely on random gen.
            int i = 1;
            string name = _saveNameBase + i;
            while ( SaveExists( name ) )
            {
                i++;
                name = _saveNameBase + i;
            }
            return name;
        }

        public override string Label
        {
            get
            {
                return "FM.ImportExport".Translate();
            }
        }

        public override ManagerJob Selected
        {
            // not used.
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void DoWindowContents( Rect canvas )
        {
            Rect loadRect = new Rect(_margin, _margin, (canvas.width - 3 * _margin) * _loadAreaRatio, canvas.height - 2 * _margin);
            Rect saveRect = new Rect(loadRect.xMax + _margin, _margin, canvas.width - 3 * _margin - loadRect.width, canvas.height - 2 * _margin);
            Widgets.DrawMenuSection( loadRect );
            Widgets.DrawMenuSection( saveRect );

            DrawLoadSection( loadRect );
            DrawSaveSection( saveRect );
        }

        private void DrawSaveSection( Rect rect )
        {
            Rect infoRect = new Rect(rect.ContractedBy(_margin));
            infoRect.height -= 30f;
            Rect nameRect = new Rect(rect.xMin + _margin, infoRect.yMax, (rect.width - 3 * _margin) / 2, 30f - _margin);
            Rect buttonRect = new Rect(nameRect.xMax + _margin, infoRect.yMax, nameRect.width, 30f - _margin);

            StringBuilder info = new StringBuilder();
            info.AppendLine( "FM.CurrentJobs".Translate() );
            foreach( Pair<string, int> jobCount in _jobCounts )
            {
                info.AppendLine( jobCount.First + ": " + jobCount.Second );
            }

            Widgets.Label( infoRect, info.ToString() );
            GUI.SetNextControlName( "ManagerJobsNameField" );
            string name = Widgets.TextField( nameRect, _saveName );
            if( GenText.IsValidFilename( name ) )
            {
                _saveName = name;
            }
            if( GenText.IsValidFilename( _saveName ) )
            {
                if( Widgets.TextButton( buttonRect, "FM.Export".Translate() ) )
                {
                    Export( _saveName );
                }
            }
            else
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.DrawBox( buttonRect );
                Widgets.Label( buttonRect, "FM.InvalidName".Translate() );
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawLoadSection( Rect rect )
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( rect, _folder );
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private bool SaveExists( string name )
        {
            return _saveFiles.Any( save => save.FileInfo.Name == name + _saveExtension );
        }

        private void Export( string name )
        {
            bool ok = true;

            // if it exists, confirm overwrite
            if( SaveExists( name ) )
            {
                ok = false;
                Find.WindowStack.Add( new Dialog_Confirm( "FM.ConfirmOverwrite".Translate(), delegate { ok = true; }, true ));
            }

            if( ok )
            {
                try
                {
                    try
                    {
                        Scribe.InitWriting( FilePath( name ), "ManagerJobs" );
                    }
                    catch( Exception ex )
                    {
                        GenUI.ErrorDialog( "ProblemSavingFile".Translate( new object[]
                        {
                        ex.ToString()
                        } ) );
                        throw;
                    }
                    ScribeHeaderUtility.WriteGameDataHeader();

                    _jobStackIO = Manager.Get.JobStack;
                    Scribe_Deep.LookDeep<JobStack>( ref _jobStackIO, "JobStack", new object[0] );
                }
                catch( Exception ex2 )
                {
                    Log.Error( "Exception while saving jobstack: " + ex2.ToString() );
                }
                finally
                {
                    Scribe.FinalizeWriting();
                    Messages.Message( "FM.JobsExported".Translate(_jobStackIO.FullStack.Count), MessageSound.Standard );
                    Refresh();
                }
            }
        }

        private string FilePath( string name )
        {
            return _folder + "/" + name + _saveExtension;
        }
    }
}
