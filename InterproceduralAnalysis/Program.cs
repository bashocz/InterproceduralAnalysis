﻿using System;
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
        private static bool test;

        static int Main(string[] args)
        {
            //programName = args[0];
            programFile = @"C:\Users\Míša\Documents\Visual Studio 2010\Projects\InterproceduralAnalysis\InterproceduralAnalysis\program.txt";
            //printLA = arg[1];
            printLA = false;
            printSA = false;
            test = true;

            if (test)
            {
                Test t = new Test();
                t.ComputeTest(null);
            }

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

            Console.ReadKey();
            return 0;
        }
    }
}
