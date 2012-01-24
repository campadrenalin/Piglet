using System;
using System.Collections.Generic;
using System.Linq;

namespace Piglet.Lexer.Construction
{
    public class DFA : FiniteAutomata<DFA.State>
    {
        public class State : BaseState
        {
            public IEnumerable<NFA.State> NfaStates { get; set; }
            public bool Mark { get; set; }

            public State(IEnumerable<NFA.State> nfaStates)
            {
                NfaStates = nfaStates;
            }

            public NFA.State[] Move(NFA nfa, char c)
            {
                // Find transitions going OUT from this state that requires c
                return (from e in nfa.Transitions.Where(f => NfaStates.Contains(f.From) && f.ValidInput == c) select e.To).ToArray();
            }

            public override string ToString()
            {
                return string.Format( "{0} {{{1}}}", StateNumber, String.Join( ", ", NfaStates));
            }

            public override bool AcceptState
            {
                get { return NfaStates.Any(f=>f.AcceptState); }
                set {}  // Do nothing, cannot set
            }
        }

        public static DFA Create(NFA nfa)
        {
            // Get the closure set of S0
            var dfa = new DFA();
            dfa.States.Add(new State(nfa.Closure(new[] { nfa.StartState })));

            while (true)
            {
                // Get an unmarked state in dfaStates
                var t = dfa.States.FirstOrDefault(f => !f.Mark);
                if (null == t)
                {
                    // We're done!
                    break;
                }

                t.Mark = true;

                // Get the move states by stimulating this DFA state with
                // all possible characters.
                for (var c = (char)1; c < 255; ++c)
                {
                    NFA.State[] moveSet = t.Move(nfa, c);
                    if (moveSet.Any())
                    {
                        // Get the closer of the move set. This is the NFA states that will form the new set
                        IEnumerable<NFA.State> moveClosure = nfa.Closure(moveSet);
                        var newState = new State(moveClosure);

                        // See if the new state already exists. If so change the reference to point to 
                        // the already created object, since we will need to add a transition back to the same object
                        newState = dfa.States.FirstOrDefault(f => f.NfaStates.Except(newState.NfaStates).Count() == 0 &&
                                                                  newState.NfaStates.Except(f.NfaStates).Count() == 0) ??
                                   newState;

                        dfa.States.Add(newState);
                        dfa.Transitions.Add(new Transition<State>(t, c, newState));
                    }
                }
            }

            dfa.StartState = dfa.States[0];
            dfa.AssignStateNumbers();
            return dfa;
        }
    }
}