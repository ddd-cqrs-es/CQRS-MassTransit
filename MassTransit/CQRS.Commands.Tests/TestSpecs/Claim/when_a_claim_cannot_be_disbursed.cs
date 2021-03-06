﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CQRS.Commands;
using CQRS.Domain.Claims;
using CQRS.Domain.Repositories;
using CQRS.DomainTesting;
using CQRS.Interfaces.Events;
using CQRS.Messages.Events;
using MHM.WinFlexOne.CQRS.Dtos;

namespace CQRS.Domain.Tests.TestSpecs.Claim
{
    public class when_a_claim_cannot_be_disbursed : EventSpecification<DisburseClaim>
    {
        private readonly string _claimId = Guid.NewGuid().ToString();
        private readonly string _participantId = Guid.NewGuid().ToString();
        private readonly decimal _claimAmount = 1200;
        private readonly string _claimType = "Vision Copay";

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
                    new ElectionDto {Id = "election1", BenefitType = "Medical"},
                    new ElectionDto {Id = "election2", BenefitType = "Dental"},
                },
                electionId => new ElectionBalanceDto
                    {
                        BalanceRemaining = 10000,
                        ElectionId = "election2",
                        ParticipantId = _participantId,
                        
                    }
            );

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
                            Reason = string.Format("No election matches the claim type '{0}'", _claimType),
                            Version = 1
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
