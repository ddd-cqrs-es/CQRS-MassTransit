﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using CQRS.DomainTesting;
using MHM.WinFlexOne.CQRS.Commands;
using MHM.WinFlexOne.CQRS.Domain.Claims;
using MHM.WinFlexOne.CQRS.Domain.Repositories;
using MHM.WinFlexOne.CQRS.Dtos;
using MHM.WinFlexOne.CQRS.Events;
using MHM.WinFlexOne.CQRS.Interfaces.Events;

namespace MHM.WinFlexOne.CQRS.Domain.Tests.TestSpecs.Claim
{
    public class when_a_claim_exceeds_the_election_balance : EventSpecification<DisburseClaim>
    {
        private readonly string _claimId = Guid.NewGuid().ToString();
        private readonly string _participantId = Guid.NewGuid().ToString();
        private readonly decimal _claimAmount = 1200;
        private readonly string _claimType = "Dental Copay";
        private readonly decimal _electionBalance = 1000;

        public override IEnumerable<IEvent> Given()
        {
            return new IEvent[]
                {
                    new ClaimRequestAutoSubstantiatedEvent
                        {
                            ClaimRequestId = _claimId,
                            ClaimType = _claimType,
                            Amount = _claimAmount,
                            ParticipantId = _participantId,
                        },
                };
        }

        public override DisburseClaim When()
        {
            return new DisburseClaim
            {
                ClaimId = _claimId,
                ParticipantId = _participantId,
            };
        }

        public override Interfaces.Commands.Handles<DisburseClaim> BuildCommandHandler()
        {
            var fakeElectionService = new FakeElectionsService(participantId => new ElectionDto[]
                {
                    new ElectionDto{ Id = "election1", BenefitType = "Medical" },
                    new ElectionDto{ Id = "election2", BenefitType = "Dental"}, 
                },
                electionId => new ElectionBalanceDto
                    {
                        BalanceRemaining = _electionBalance,
                        ElectionId = "election2",
                        ParticipantId = _participantId,
                    });

            return new DisburseClaimCommandHandler(new Repository<ClaimRequest>(EventStore), fakeElectionService);
        }

        public override IEnumerable<IEvent> Then()
        {
            return new IEvent[]
                {
                    new ClaimNotDisbursedEvent
                        {
                            ClaimAmount = _claimAmount,
                            ClaimId = _claimId,
                            ClaimType = _claimType,
                            Reason = string.Format(
                            "Your remaining election balance is {0} which is not enough to cover your claim for {1}", 
                            _electionBalance, _claimAmount)
                        }
                };
        }

        public override Expression<Predicate<Exception>> ThenException()
        {
            return NoException;
        }

        public override void Finally()
        {
            //nought to do
        }
    }
}