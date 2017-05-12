using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Generated
{
    abstract class IPDPlayerGeneratedNode
    {
        public delegate IPDGame.Choices[] QFunction(int alpha, int beta);

        /// <summary>
        /// This Evaluate function should only be called on the root node by IPDPlayers
        /// </summary>
        /// <param name="game">Current game being played.</param>
        /// <param name="result">Sequence of choices to be made.</param>
        /// <returns>Whether it succesfully returned a sequence.</returns>
        public IPDPlayerGeneratedTreeIterator Evaluate(IPDGame game)
        {
            return Evaluate(game, 0, 0);    //many options how to continue..
        }

        /// <summary>
        /// This Evaluate function should only be called internally.
        /// </summary>
        public abstract IPDPlayerGeneratedTreeIterator Evaluate(IPDGame game, int Alpha, int Beta);

        protected int _depth;
    }
}
