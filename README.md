#### Basic functionality for INTEL:

1. Correct domain is already selected: IPD Problem (Novelty NEAT)
2. Press "Load Domain Default Parameters", then press "Create Random Population"
3. Start, to start the experiment.

- To view the current best genome, go to View > Best Genome for a diagram
- To view detailed information and progress on the current experiment, go to View > Problem Domain

Other options in the View panel are non-functional.

Please note that the program has not been thoroughly tested for edge-case scenarios, and that crashes and freezes could happen upon performing unexpected operations.

#### To change parameters as per Appendix B.2:

* Change the Population Size in the main screen, on Page 1.
* Change Evolution Specific (excluding the Activation Scheme) parameters on Page 2.
* Change Genome Specific parameters on Page 2.
* Change Domain Specific parameters in SharpNeatDomains.experiments.xml, lines 4 - 71 are relevant. Restart the program after saving changes in this xml file.

# SharpNEAT - Evolution of Neural Networks

NEAT is NeuroEvolution of Augmenting Topologies; an evolutionary algorithm devised by Kenneth O. Stanley. 

SharpNEAT is a complete implementation of NEAT written in C# and targeting .NET (on both MS Windows and Mono/Linux).

From the [SharpNEAT FAQ](http://sharpneat.sourceforge.net/faq.html)...

#### 1. What is SharpNEAT

In a nutshell, SharpNEAT provides an implementation of an Evolutionary Algorithm (EA) with the specific goal of evolving neural networks. The EA uses the evolutionary mechanisms of mutation, recombination and selection to search for neural networks with behaviour that satisfies some formally defined problem. Example problems might be how to control the limbs of a simple biped or quadruped to make it walk, how to control a rocket to maintain vertical flight, or finding a network that implements some desired digital logic (such as a multiplexer).

A notable point is that NEAT and SharpNEAT search both neural network structure (network nodes and connectivity) and connection weights (inter-node connection strength). This is distinct from algorithms such as back-propogation that generally attempt to discover good connection weights for a given structure.

SharpNEAT is a framework that facilitates research into evolutionary computation and specifically evolution of neural networks. The framework provides a number of example problem domains that demonstrate how it can be used to produce a complete working EA. SharpNEAT is modular and therefore an alternative genetic coding or entire new evolutionary algorithm can be used alongside the wider framework. The provision for such modular experimentation was a major design goal of SharpNEAT and is facilitated by abstractions made in SharpNEAT's architecture around key concepts such as genome (genetic representation and coding) and evolutionary algorithm (mutations, recombination, selection strategy).

Motivation for the development of SharpNEAT mainly came from a broader interest in biological evolution, and more specifically curiosity on what the limits of neuro-evolution are in terms of the level of problem complexity it can produce satisfactory solutions for.


---

Donate! Become a Patreon Sponsor at https://www.patreon.com/sharpneat
