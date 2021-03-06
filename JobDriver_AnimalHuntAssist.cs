using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using RimWorld;

namespace Nandonalt_Limbs
{
    public class JobDriver_AnimalHuntAssist : JobDriver
    {
        public const TargetIndex PreyInd = TargetIndex.A;

        private const TargetIndex CorpseInd = TargetIndex.A;

        private const int MaxHuntTicks = 5000;

        private bool notifiedPlayer;

        private bool firstHit = true;

        public Pawn Prey
        {
            get
            {
                Corpse corpse = this.Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }
                return (Pawn)base.CurJob.GetTarget(TargetIndex.A).Thing;
            }
        }

        private Corpse Corpse
        {
            get
            {
                return base.CurJob.GetTarget(TargetIndex.A).Thing as Corpse;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<bool>(ref this.firstHit, "firstHit", false, false);
        }

        public override string GetReport()
        {
            if (this.Corpse != null)
            {
                return base.ReportStringProcessed(JobDefOf.HaulToCell.reportString);
            }
            return base.GetReport();
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            base.AddFinishAction(delegate
            {
                this.Map.attackTargetsCache.UpdateTarget(this.pawn);
            });
            Toil prepareToEatCorpse = new Toil();
            prepareToEatCorpse.initAction = delegate
            {
                Pawn actor = this.pawn;
                Pawn prey = this.Prey;
                if (prey == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    return;
                }
                Corpse corpse = prey.Corpse;
                if (corpse == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    return;
                }
                if (actor.Faction == Faction.OfPlayer)
                {
                    corpse.SetForbidden(false, false);
                }
                else
                {
                    corpse.SetForbidden(true, false);
                }
                actor.CurJob.SetTarget(TargetIndex.A, corpse);
            };
            yield return new Toil
            {
                initAction = delegate
                {
                    this.Map.attackTargetsCache.UpdateTarget(this.pawn);
                },
                atomicWithPrevious = true,
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            Action onHitAction = delegate
            {
                Pawn prey = this.Prey;
                bool surpriseAttack = this.firstHit && !prey.IsColonist;
                if (this.pawn.meleeVerbs.TryMeleeAttack(prey, this.CurJob.verbToUse, surpriseAttack))
                {
                    if (!this.notifiedPlayer && PawnUtility.ShouldSendNotificationAbout(prey))
                    {
                        this.notifiedPlayer = true;
                        Messages.Message("MessageAttackedByPredator".Translate(new object[]
                        {
                            prey.LabelShort,
                            this.pawn.LabelIndefinite()
                        }).CapitalizeFirst(), prey, MessageSound.SeriousAlert);
                    }
                    this.Map.attackTargetsCache.UpdateTarget(this.pawn);
                }
                this.firstHit = false;
            };
             yield return Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, onHitAction).EndOnDespawnedOrNull(TargetIndex.A).FailOn(() => Find.TickManager.TicksGame > this.startTick + 5000 && (this.CurJob.GetTarget(TargetIndex.A).Cell - this.pawn.Position).LengthHorizontalSquared > 4f);
            //yield return prepareToEatCorpse;
            //Toil gotoCorpse = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            //yield return gotoCorpse;
            // float durationMultiplier = 1f / this.pawn.GetStatValue(StatDefOf.EatingSpeed, true);
            //yield return Toils_Ingest.ChewIngestible(this.pawn, durationMultiplier, TargetIndex.A, TargetIndex.None).FailOnDespawnedOrNull(TargetIndex.A);
            //yield return Toils_Ingest.FinalizeIngest(this.pawn, TargetIndex.A);
            //yield return Toils_Jump.JumpIf(gotoCorpse, () => this.pawn.needs.food.CurLevelPercentage < 0.9f);
   
        }

    
        }
    }


