using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterproceduralAnalysis
{
    class Program
    {
        private static string programFile;
        private static bool printLA;
        private static bool printSA;
        private static bool printIAM;
        private static bool printIAG;
        private static bool printIALE;

        static int Main(string[] args)
        {
            //TestLR test = new TestLR();
            //test.Testuj();
            //Console.ReadKey();
            //return 0;

            //programName = args[0];
            programFile = @"d:\Projects\github\InterproceduralAnalysis\InterproceduralAnalysis\program.txt";
            //printLA = arg[1];
            printLA = false;
            printSA = false;
            printIAM = true;
            printIAG = true;
            printIALE = true;

            if (programFile == null)
            {
                Console.WriteLine("Parametr 'programFile' je povinny.");
                // PrintHelp();
                Console.ReadKey();
                return -1;
            }

            if (!File.Exists(programFile))
            {
                Console.WriteLine(string.Format("Soubor programu '{0}' neexistuje.", programFile));
                // PrintHelp();
                Console.ReadKey();
                return -1;
            }

            LexicalAnalyzer la = new LexicalAnalyzer(programFile, printLA);
            SyntacticAnalyzer sa = new SyntacticAnalyzer(la);
            ProgramAst prg;
            if (!sa.GetAST(out prg))
            {
                Console.ReadKey();
                return -1;
            }

            SemanticAnalyzer sea = new SemanticAnalyzer();
            if (!sea.CheckAST(prg))
            {
                Console.ReadKey();
                return -1;
            }

            Console.WriteLine("Pocet promennych: {0}, pocet funkci: {1}", prg.VarsDecl.Count, prg.OrigFncs.Count);

            StatementConverter sc = new StatementConverter(printSA);
            sc.ConvertToIfGoto(prg);

            GraphGenerator gg = new GraphGenerator();
            gg.CreateGraph(prg);

            int w = 4;
            int n = prg.VarsDecl.Count;
            InterproceduralAnalyzer ia = new InterproceduralAnalyzer(w, n, printIAM, printIAG, printIALE);
            ia.Analyze(prg);

            WriteProgram wp = new WriteProgram();
            wp.Write(prg, programFile);

            Console.WriteLine("Konec analyzy... stiskni klavesu.");
            Console.ReadKey();
            return 0;
        }
    }
}
