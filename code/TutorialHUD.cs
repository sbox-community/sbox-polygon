using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

//TODO
public partial class TutorialHUD : WorldPanel
{
    public static TutorialHUD self;
    public TutorialHUD()
    {
        self = this;
        SpawnTutorialPanels();
    }

    public void SpawnTutorialPanels()
    {
    }

    public void DeleteAll()
    {
    }
}
