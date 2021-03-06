﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piglet.Lexer.Construction
{
    internal abstract class FiniteAutomata<TState> where TState : FiniteAutomata<TState>.BaseState
    {
        public abstract class BaseState
        {
            public abstract bool AcceptState { get; set; }
            public int StateNumber { get; set; }
        }

        public IList<TState> States { get; set; }
        public IList<Transition<TState>>  Transitions { get; set; }
        public TState StartState { get; set; }


        protected FiniteAutomata()
        {
            States = new List<TState>();
            Transitions = new List<Transition<TState>>();
        }

        public abstract IEnumerable<TState> Closure(TState[] states, ISet<TState> visitedStates = null);

        public void AssignStateNumbers()
        {
            int i = 0;
            foreach (var state in States)
            {
                if (state != StartState)
                    state.StateNumber = ++i;
            }
            // Always use 0 as the start state
            StartState.StateNumber = 0;
        }

        public void DistinguishValidInputs()
        {
            // For each transition which has valid inputs, if any other state has a valid input range which in any way 
            // intersects with the valid inputs of this range, create two ranges instead.
            bool changes;
            do
            {
                changes = false;
                foreach (var t1 in Transitions)
                {
                    Transition<TState> t = t1;
                    foreach (var t2 in Transitions.Where(f => f != t))
                    {
                        changes |= t.ValidInput.DistinguishRanges(t2.ValidInput);
                    }
                }
            } while (changes);

        }

        public StimulateResult<TState> Stimulate(string input)
        {
            var activeStates = Closure(new[] {StartState}).ToList();
            var matchedString = new StringBuilder();
            foreach (var c in input)
            {
                var toStates = new HashSet<TState>();
                foreach (var activeState in activeStates)
                {
                    var nextStates = Transitions.Where(t => t.From == activeState && t.ValidInput.ContainsChar(c)).Select(t=>t.To);
                    toStates.UnionWith(nextStates);
                }

                if (toStates.Any())
                {
                    matchedString.Append(c);
                    activeStates = Closure(toStates.ToArray()).ToList();
                }
                else
                {
                    break;
                }
            }

            return new StimulateResult<TState>
                       {
                           Matched = matchedString.ToString(),
                           ActiveStates = activeStates
                       };
        }
    }

    internal class StimulateResult<TState> where TState : FiniteAutomata<TState>.BaseState
    {
        public string Matched { get; set; }
        public IEnumerable<TState> ActiveStates { get; set; }
    }
}
