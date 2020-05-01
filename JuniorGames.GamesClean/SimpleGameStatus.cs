namespace JuniorGames.GamesClean
{
    using System.Collections.Generic;
    using GameBox.Framework;

    public class SimpleGameStatus
    {
        public SimpleGameStatus(List<ILightableButton> chain)
        {
            this.Chain = chain;
            this.FaultCounter = 0;
            this.InputIndex = 0;
        }

        public int InputIndex { get; private set; }

        public List<ILightableButton> Chain { get; }

        public int FaultCounter { get; private set; }

        public ButtonIdentifier ExpectedButton => this.Chain[this.InputIndex].ButtonIdentifier;

        public void IncreaseFaultCounter()
        {
            this.FaultCounter++;
        }

        public void IncreaseInputIndex()
        {
            this.InputIndex++;
        }

        public void ResetInputIndex()
        {
            this.InputIndex = 0;
        }

        public void ResetFaultCounter()
        {
            this.FaultCounter = 0;
        }
    }
}