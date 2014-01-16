/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved. 
 *******************************************************************************/
using VDS.RDF;
namespace org.topbraid.spin.arq
{

    /**
     * An abstract superclass for Functions with 1 argument.
     * 
     * @author Holger Knublauch
     */
    public abstract class AbstractFunction1 : AbstractFunction
    {

        override protected NodeValue exec(INode[] nodes, FunctionEnv env)
        {
            INode arg1 = nodes.Length > 0 ? nodes[0] : null;
            return exec(arg1, env);
        }


        protected abstract NodeValue exec(INode arg1, FunctionEnv env);
    }
}