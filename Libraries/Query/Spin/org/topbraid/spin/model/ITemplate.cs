/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved. 
 *******************************************************************************/
using System;
namespace org.topbraid.spin.model
{


    /**
     * A template class definition.
     * 
     * @author Holger Knublauch
     */
    public interface ITemplate : IModule
    {

        /**
         * Gets the declared spin:labelTemplate (if any exists).
         * @return the label template string or null
         */
        String getLabelTemplate();
    }
}