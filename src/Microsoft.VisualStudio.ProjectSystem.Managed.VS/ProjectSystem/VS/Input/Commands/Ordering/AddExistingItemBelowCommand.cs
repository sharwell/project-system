﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(CommandGroup.ManagedProjectSystemOrder, ManagedProjectSystemOrderCommandId.AddExistingItemBelow)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class AddExistingItemBelowCommand : AbstractAddItemCommand
    {
        [ImportingConstructor]
        public AddExistingItemBelowCommand(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            OrderAddItemHintReceiver orderAddItemHintReceiver) :
            base(projectTree, projectVsServices, serviceProvider, orderAddItemHintReceiver)
        {
        }

        protected override Task OnAddingNodesAsync(IProjectTree nodeToAddTo)
        {
            return ShowAddExistingFilesDialogAsync(nodeToAddTo);
        }

        protected override bool CanAdd(IProjectTree target)
        {
            return OrderingHelper.HasValidDisplayOrder(target);
        }

        protected override OrderingMoveAction Action => OrderingMoveAction.MoveBelow;
    }
}
