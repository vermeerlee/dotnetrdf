/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved. 
 *******************************************************************************/
using VDS.RDF.Query.Spin.SparqlUtil;
using VDS.RDF;
using VDS.RDF.Query.Spin;
using VDS.RDF.Query.Datasets;

namespace org.topbraid.spin.model.impl
{
    public class TripleTemplateImpl : TripleImpl, ITripleTemplate
    {

        public TripleTemplateImpl(INode node, SpinProcessor spinModel)
            : base(node, spinModel)
        {
        }


        override public void print(IContextualSparqlPrinter p)
        {
            p.setNamedBNodeMode(true);
            base.print(p);
            p.setNamedBNodeMode(false);
        }
    }
}