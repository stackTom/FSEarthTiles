using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthTilesDLL
{
    public interface FSEarthTilesInterface
    {

        void ProcessHotPlugInConfigList(List<String> iDirectConfigurationList);

        void SetArea(Double iNWLatitude, Double iNWLongitude, Double iSELatitude, Double iSELongitude);

        //Only call SetReferenceArea if you don't want to work with the Auto Reference Mode
        void SetReferenceArea(Double iNWLatitude, Double iNWLongitude, Double iSELatitude, Double iSELongitude);

        void Start();
        void Abort();

        Boolean IsBusy();             //Most function will automatical Sleep (autocall WaitTillDoneAndReady) when you call them and FSET is still busy.
        void WaitTillDoneAndReady();  //But you want to spend the time better then you should poll with IsBusy();

        void SetAutoMode();            //Set's Auto Reference Mode
        void SetFixMode();             //Set's Fix Mode

        String GetAreaFileNameMiddlePart();
        String GetWorkingFolder();
        String GetSceneryFolder();
        String GetFSEarthTilesApplicationFolder();
    }
}
