using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The morning's post. Each letter is a sheet on its sender's letterhead: the coroner's office
/// with its seal, a diploma mill's flourish, a customer's own hand. Replies are slips under the
/// letter. Answering stamps the sheet and folds it away, and says at once what the answer cost.
/// </summary>
public partial class LettersDesk : VBoxContainer
{
	private readonly IGame _game;
	private string? _note;
	public event Action? Changed;

	/// <summary>
	/// Who writes each kind of letter, and what they write, in their own voice. The docket is the
	/// clerk's note of how it came (set in italic above the sheet); {name} is the person the letter
	/// concerns, read off its title.
	/// </summary>
	private sealed record Head(string Sender, string Address, string Sign, string Seal, string Docket, string Salute, string Voice);

	private static readonly Dictionary<string, Head> Heads = new()
	{
		["inquest"] = new("OFFICE OF THE CORONER", "City and County of New-York · Centre Street", "By order of the Coroner, J. DUFFY, Clerk", "seal_coroner",
			"Served at the shop by a boy from Centre street.", "Sir,—",
			"You are hereby notified that a jury will sit this month upon the death of {name}, who is said to have been a customer of yours. The Coroner will hear any evidence you may wish to give as to the composition of your preparations. The Coroner's time is valuable, as is his good opinion."),
		["grateful_letter"] = new("", "Mulberry Street, top floor, rear", "Your grateful servant, a customer", "",
			"In a careful hand, on good paper bought for the purpose.", "Dear Doctor,—",
			"Three physicians gave me up, and your tonic gave me back. I am a new woman and my husband says he hardly knows me. If my words would be of use to other sufferers, you have my leave to put them in the papers, with my name, which I should like spelled correctly."),
		["diploma_mill"] = new("THE ECLECTIC MEDICAL COLLEGE OF OHIO", "Diplomas Conferred by Post · Cincinnati", "J. T. MARSH, M.D. (Hon.), Registrar", "seal_college",
			"A printed circular, with the blanks filled in by hand.", "Dear Doctor,—",
			"The Faculty have heard of your work with great interest, and are pleased to offer you the degree of Doctor of Medicine on receipt of fifty dollars. Attendance is not required; examination is waived in cases of evident merit, and all cases are evident. The diploma is suitable for framing."),
		["sanitary_inspector"] = new("DEPARTMENT OF HEALTH · SANITARY BUREAU", "301 Mott Street", "P. QUINLAN, Inspector of Nuisances", "seal_sanitary",
			"Left on the counter by a man in a hard hat, who is still in the back room.", "Sir,—",
			"I am directed to make an inspection of your back premises. Inspections of this kind are thorough, and can run on for weeks, during which the ward will see a Board of Health man in your back room, and talk of it. A small consideration for my trouble has been known to shorten them."),
		["rival_shop_opens"] = new("", "A note pushed under the door", "A friend of the shop", "",
			"Unsigned, in pencil, with a thumbprint.", "",
			"Thought you ought to know. Feeney has opened two doors down and his window is full of bottles the very spit of yours, at a penny less. People will say anything about a man's stock, if they are given a start."),
		["temperance_picket"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by a lady, while a dozen more sing hymns on the step.", "Sir,—",
			"The League has caused your remedy to be tasted by a member, who has not been well since. We shall sing outside your door each day until you see the error of your trade, or contribute to the cause of temperance, which we should accept as a first step."),
		["board_of_health"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Order of the Board", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"Served with a warrant, by two inspectors and a wagon.", "Sir,—",
			"Complaint having been made of your preparations, the Board directs that the whole of your stock be removed for analysis. The Board will, in the alternative, accept a fine of thirty dollars, and consider the complaint answered."),
		["rival_price_war"] = new("", "A handbill, pushed under the door", "J. FEENEY, Druggist, two doors down", "",
			"Found on the mat, with three more like it in the street.", "",
			"FEENEY'S TONIC, SIXTY-FIVE CENTS THE BOTTLE. Why pay more on Mulberry street? For the next four months, two callers in five whose bottle costs more than mine will try mine instead. Ask anyone. — J. Feeney"),
		["landlord_rent"] = new("", "Upstairs, the front room", "Your obedient servant, M. O'ROURKE, Landlord", "",
			"Brought down by the landlord's boy, who waited for an answer.", "Sir,—",
			"I see the trade is good, and I am glad of it. From this month the rent is a dollar more, and it will stay so. I would sell you the lease outright for sixty dollars, which would bring it a dollar under the old rent for good. The narrow shop across the street is to let at a dollar under the old rent, if you can do with room for eight fewer regulars."),
		["spirits_dearer"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with a note in the margin.", "Sir,—",
			"We regret that the distillers have combined, and that grain spirit will be charged at double the old price for six months from your acknowledgment. Should you prefer it, a Jersey distiller outside the ring will supply at the old price for six dollars down; we are not to be named in the matter."),
		["benefit_society"] = new("ST. BRIDGET'S MUTUAL AID SOCIETY", "Meetings Tuesdays · over the bakery, Mott Street", "Your servant, the Secretary (a regular of the shop)", "",
			"Handed across the counter by one of your regulars, in her Sunday hat.", "Dear Doctor,—",
			"The Society would recommend your remedy to its members in return for a subscription of a dollar and a half a month for the year. We think you may count on five more callers a month. The Society asks only that the remedy do its members no harm, which we are sure it will not."),
		["customer_wants_stronger"] = new("", "In pencil, on the back of a handbill", "One of your regulars", "",
			"Left on the counter, folded small.", "",
			"Doc, the tonic dont do what it did. I take two spoons and then four and its like water. You must have something stronger in the back. I can pay. I have to have it."),
		// Events pass 1: the letters of the long middle (playtests/events-pass-1.md).
		["landlord_roof"] = new("", "Upstairs, the front room", "Your obedient servant, M. O'ROURKE, Landlord", "",
			"Brought down by the landlord's boy, with a wet sleeve to show.", "Sir,—", ""),
		["doctor_arrangement"] = new("LUCIUS FENN, M.D.", "Physician & Surgeon · 212 Grand Street · Hours 8 to 10 and 6 to 8", "L. FENN, M.D.", "",
			"On a physician's letter paper, very white.", "Dear Sir,—", ""),
		["parish_bazaar"] = new("ST. URSULA'S CHAPEL", "Mott Street · The Rectory", "Yours in Christ, D. GARVEY, Rector", "",
			"Brought by two children selling raffle tickets, who also sold you one.", "Dear Sir,—", ""),
		["police_collection"] = new("", "Left at the counter by a patrolman, off his beat", "J. MORAN, Wardman, for the Captain", "",
			"Pencilled on the back of a summons form.", "Friend,—", ""),
		["gazette_canvasser"] = new("THE BOWERY GAZETTE", "Advertising Department · 18 Park Row", "H. LINDLEY, Canvasser", "",
			"Brought by the canvasser himself, who stayed for a bottle.", "Sir,—", ""),
		["board_of_pharmacy"] = new("THE BOARD OF PHARMACY OF THE CITY OF NEW-YORK", "Office of the Secretary · Bond Street", "By order of the Board, R. AULT, Secretary", "",
			"A printed form, with your name spelled two ways.", "Sir,—", ""),
		["blizzard_1888"] = new("THE RELIEF COMMITTEE OF THE WARD", "Meeting at the Elizabeth Street station-house", "For the Committee, W. HALLORAN, Secretary", "",
			"Carried over the drifts by a boy in his father's boots.", "Sir,—", ""),
		["grippe_1889"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with a sample placard folded in.", "Sir,—", ""),
		["jobber_exclusive"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"Brought by the Hobarts' traveller, with a fountain pen held out.", "Sir,—", ""),
		["medicine_show_offer"] = new("PROF. GIDEON MARSH'S MEDICINE CONCERT & COMEDY CO.", "On the road · Letters care of the Postmaster, Elmira", "G. MARSH, Lecturer & Proprietor", "",
			"On a handbill, with the Professor's portrait in a frock coat.", "My dear Sir,—", ""),
		["medicine_show_returns"] = new("PROF. GIDEON MARSH'S MEDICINE CONCERT & COMEDY CO.", "Winter quarters · Elmira, N.Y.", "G. MARSH, Lecturer & Proprietor", "",
			"A registered letter, with the money in it, mostly in ones.", "My dear Partner,—", ""),
		["kehoe_thanks"] = new("", "Mulberry Street, top floor, front", "Respectfully, Mrs. M. KEHOE", "",
			"In a careful hand, on a sheet torn from a copybook.", "Dear Sir,—", ""),
		["kehoe_more"] = new("", "Mulberry Street, top floor, front", "Respectfully, Mrs. M. KEHOE", "",
			"The same careful hand, the same copybook.", "Dear Sir,—", ""),
		["kehoe_last"] = new("", "Mulberry Street, top floor, rear", "Mrs. B. DOYLE, for Mrs. Kehoe", "",
			"Another hand. The empty bottle came with it.", "", ""),
		["kehoe_well"] = new("", "Mulberry Street, top floor, front", "Respectfully, Mrs. M. KEHOE", "",
			"The careful hand again, on better paper.", "Dear Sir,—", ""),
		["stowe_tract"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by Mrs. Stowe herself, who waited by the door.", "Sir,—", ""),
		["ward_boss_favor"] = new("", "Kelly's back room, any night after six", "ED CASSIDY, Captain of the District, for the Alderman", "",
			"Scrawled on the back of a sample ballot.", "Friend,—", ""),
		["ward_boss_due"] = new("", "Kelly's back room, any night after six", "ED CASSIDY, Captain of the District, for the Alderman", "",
			"Scrawled on a sample ballot, with the Alderman's name ticked.", "Friend,—", ""),
		["feeney_boy"] = new("", "Under the counter, where he leaves his cap", "T. BURKE (your boy)", "",
			"Left in the till, folded around a penny.", "Mister,—", ""),
		["feeney_copy"] = new("", "A handbill, pushed under the door", "J. FEENEY, Druggist, two doors down", "",
			"Found on the mat; the boy's cap was on the corner of Feeney's counter.", "", ""),
		["feeney_inquest"] = new("OFFICE OF THE CORONER", "City and County of New-York · Centre Street", "By order of the Coroner, J. DUFFY, Clerk", "seal_coroner",
			"Served at the shop by a boy from Centre street, who asked for Feeney's.", "Sir,—", ""),
		["pawn_ticket"] = new("D. CRANE, LICENSED PAWNBROKER", "Loans on Watches, Rings & Wearing Apparel · Bayard Street", "D. CRANE", "",
			"A pawn ticket pinned to a printed card.", "Sir,—", ""),
		["widow_account"] = new("", "Elizabeth Street, second floor, rear", "Respectfully, a widow of the ward", "",
			"In a careful hand, with black edges cut from a funeral card.", "Dear Sir,—", ""),
		["soda_fountain_offer"] = new("THE CRYSTAL FOUNT COMPANY", "Soda Water Apparatus · 14 Gold Street", "per W. LOOMIS, Agent", "",
			"With a lithograph of the fountain, and a lady drinking from it.", "Sir,—", ""),
		["undertaker"] = new("SILAS VOORHEES", "Undertaker & Embalmer · Funerals Furnished Complete · Bayard Street", "S. VOORHEES", "",
			"On black-edged paper, by a man in black gloves.", "Dear Sir,—", ""),
		["electric_light"] = new("THE MANHATTAN ELECTRIC ILLUMINATING CO.", "Contract Department · Pearl Street", "per A. STRAUB, Contract Agent", "",
			"With a pamphlet on the safety of the electric current.", "Sir,—", ""),
		["reporter_calls"] = new("THE EVENING CLARION", "City Room · 41 Park Row", "NED PRYOR, of the Clarion", "",
			"Dashed off on copy paper, and delivered by a boy who wanted a reply.", "Dear Doc,—", ""),
		["reporter_proofs"] = new("THE EVENING CLARION", "City Room · 41 Park Row", "NED PRYOR, of the Clarion", "",
			"A galley proof, still damp, with a note pinned to it.", "Dear Doc,—", ""),
		["reporter_kind"] = new("THE EVENING CLARION", "City Room · 41 Park Row", "NED PRYOR, of the Clarion", "",
			"With the cutting enclosed, and a cigar ash on it.", "Dear Doc,—", ""),
		["stowe_chemist"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by Mrs. Stowe, with the chemist's certificate held back.", "Sir,—", ""),
		["stowe_petition"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Brought by Mrs. Stowe, with the petition rolled under her arm.", "Dear Sir,—", ""),
		["proprietary_invite"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"An engraved invitation, with a printed list of the bills it has killed.", "Dear Sir,—", ""),
		["red_clause"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"A circular to members, with the clause printed in red.", "Dear Sir,—", ""),
		["cholera_1892"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Circular to Druggists", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"A printed circular, with a printed notice folded inside.", "Sir,—", ""),
		["magazine_household"] = new("THE HEARTHSIDE MONTHLY", "Household Department · Union Square", "MISS E. PRATT, Editress of the Household Page", "",
			"On lilac paper, smelling faintly of violets.", "Dear Sir,—", ""),
		["panic_relief"] = new("THE EAST SIDE RELIEF WORK COMMITTEE", "Rooms at the University Settlement · Delancey Street", "For the Committee, S. GOLDING, Secretary", "",
			"Brought by a man who said he was a cabinetmaker, and was.", "Sir,—", ""),
		["panic_credit"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with 'URGENT' in the junior partner's hand.", "Sir,—", ""),
		["lexow_subpoena"] = new("SPECIAL COMMITTEE OF THE SENATE TO INVESTIGATE THE POLICE DEPARTMENT", "Sitting at the County Court House · Chambers Street", "H. VAIL, Clerk to the Committee", "",
			"Served by a process server, who had a list of the street.", "Sir,—", ""),
		["sunday_closing"] = new("", "The Mulberry Bend Café, Lager & Wines, shut Sundays by order", "G. RITTER, Proprietor", "",
			"Brought round on a Sunday, by the side door.", "Neighbour,—", ""),
		["wheelmen"] = new("THE MULBERRY WHEELMEN", "Club-room over the bakery, Grand Street · Runs Sundays at 7", "F. ACKERLY, Captain of the Road", "",
			"Delivered by a young man in knickerbockers, who rode it in.", "Dear Sir,—", ""),
		["trolley_cards"] = new("THE STREET RAILWAY ADVERTISING COMPANY", "Car Cards & Station Space · Nassau Street", "per J. HENNESSY, Solicitor", "",
			"With a sample card, in three colours.", "Sir,—", ""),
		["bicycle_face"] = new("ORRIN GILCHRIST, M.D.", "Specialist in Nervous Diseases · West Twenty-third Street", "O. GILCHRIST, M.D.", "",
			"Typewritten, and signed with a flourish.", "Dear Sir,—", ""),
		["state_chemist"] = new("STATE BOARD OF HEALTH", "Office of the Analyst · The Capitol, Albany", "By direction, W. CORLISS, Analyst", "",
			"An official envelope, franked.", "Sir,—", ""),
		["lodging_house"] = new("THE GLOBE LODGING HOUSE", "Bowery · Beds 10c., 15c. & 25c. · No Women", "M. KRAUSE, Proprietor", "",
			"Brought by the night clerk, who would not sit down.", "Sir,—", ""),
		["tammany_back"] = new("", "Kelly's back room, any night after six", "ED CASSIDY, Captain of the District, for the Alderman", "",
			"Scrawled on the back of a Tammany ticket.", "Friend,—", ""),
		["volunteers_1898"] = new("", "Camp Black, Hempstead Plains · Co. G, Volunteer Infantry", "J. CORCORAN, Q.M. Sergeant, Co. G", "",
			"Brought by a corporal of nineteen, in a uniform too large for him.", "Dear Sir,—", ""),
		["army_contract"] = new("B. F. HOLLISTER", "Contractor to the Commissary Department · Willard's Hotel", "B. F. HOLLISTER", "",
			"On hotel paper, with a war-stamp on the envelope.", "Dear Sir,—", ""),
		["army_inquiry"] = new("COMMISSION TO INVESTIGATE THE CONDUCT OF THE WAR DEPARTMENT", "Sitting at the War Department", "By direction, R. HALE, Recorder", "",
			"An official envelope, with the eagle on it.", "Sir,—", ""),
		["farm_wife"] = new("", "Rural Free Delivery No. 2, Licking County, Ohio", "Mrs. L. CARROW", "",
			"In pencil, on lined paper, with a two-cent stamp.", "Dear Sir,—", ""),
		["testimonial_broker"] = new("THE ELITE TESTIMONIAL BUREAU", "Letters of Cure Supplied · 117 Nassau Street", "per R. DUNNE, Manager", "",
			"A printed circular, with three sample cures in three different hands.", "Dear Sir,—", ""),
		["heroin_traveller"] = new("DYER & PLATT", "Sole Agents for Fine Chemicals of the Rhine · William Street", "per O. HALVORSEN, Traveller", "",
			"Left with a sample tin, and a pamphlet in two languages.", "Sir,—", ""),
		["letter_broker"] = new("W. GEDNEY, DEALER IN LETTERS", "Names and Addresses for Mail-Order Houses · Room 9, 150 Nassau Street", "W. GEDNEY", "",
			"A card, with a price list on the back.", "Sir,—", ""),
		["ice_trust"] = new("THE HUDSON & CROTON ICE COMPANY", "Office of the Consolidated Companies · Broadway", "By order of the Directors", "",
			"A printed card, left with the iceman's bill.", "Sir,—", ""),
		["cut_rate"] = new("THE RETAIL DRUGGISTS' PROTECTIVE ASSOCIATION", "Local Branch · Rooms at the College of Pharmacy", "For the Branch, P. LUNDQUIST, Secretary", "",
			"A circular, with a cut-rate handbill attached showing your bottle.", "Dear Sir,—", ""),
		["grimsby_belt"] = new("DR. GRIMSBY'S GALVANIC BELT CO.", "Restorers of Lost Vigor · 9 Ann Street", "H. GRIMSBY (\"Dr.\")", "",
			"With a Belt enclosed, on thirty days' trial.", "Dear Brother Proprietor,—", ""),
		["tenement_dept"] = new("TENEMENT HOUSE DEPARTMENT OF THE CITY OF NEW YORK", "Bureau of Inspection · 61 Irving Place", "By order, F. MARSH, Chief Inspector", "",
			"A printed order, with the inspector's notes in the margin.", "Sir,—", ""),
		["bureau_sample"] = new("U.S. DEPARTMENT OF AGRICULTURE", "Bureau of Chemistry", "By direction of the Chief, A. KEENE, Clerk", "",
			"An official envelope, franked, with a printed form.", "Sir,—", ""),
		["radium_water"] = new("THE RADIO-VITAL WATER COMPANY", "Laboratories at Newark, N.J.", "per E. FAIRBAIRN, Secretary", "",
			"With a photograph of a glowing bottle, which is a painting.", "Dear Sir,—", ""),
		["motor_car"] = new("BRILL & ASHBY", "Attorneys and Counsellors · 2 Wall Street", "BRILL & ASHBY, for the owner", "",
			"Delivered by a clerk, who looked at the hole in the window and coughed.", "Sir,—", ""),
		["subway_signs"] = new("THE RAPID TRANSIT ADVERTISING CONCESSION", "Station & Car Space · Broadway", "per L. MCALLISTER, Agent", "",
			"With a drawing of the platform, and a sign on every pillar.", "Sir,—", ""),
		["muckraker"] = new("", "Editorial Rooms, a national weekly · Fifth Avenue", "M. ORCUTT, Special Correspondent", "",
			"Typewritten, with a list of questions numbered one to nine.", "Dear Sir,—", ""),
		["reporter_returns"] = new("", "Editorial Rooms, a national weekly · Fifth Avenue", "NED PRYOR, late of the Clarion", "",
			"Typewritten now, but signed in the same hand.", "Dear Doc,—", ""),
		["ama_council"] = new("AMERICAN MEDICAL ASSOCIATION", "Council on Pharmacy and Chemistry · Dearborn Avenue", "By direction of the Council, the Secretary", "",
			"A printed form, with a long list of questions.", "Sir,—", ""),
		["pa_war_chest"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"Marked PERSONAL AND CONFIDENTIAL, twice.", "Dear Sir,—", ""),
		["red_clause_called"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"Marked PERSONAL AND CONFIDENTIAL, three times.", "Dear Sir,—", ""),
		["congress_member"] = new("HOUSE OF REPRESENTATIVES", "Office of the Member for your District", "J. WIMBUSH, Secretary to the Member", "",
			"On House paper, franked.", "Dear Sir,—", ""),
		["stowe_washington"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"In an old lady's hand, slower now, but every word legible.", "Dear Sir,—", ""),
		["label_lawyer"] = new("GRUE & PARTRIDGE", "Counsellors-at-Law · Specialists to the Proprietary Trade · 32 Liberty Street", "GRUE & PARTRIDGE", "",
			"On heavy paper, with the specimen label pasted on, upside down.", "Dear Sir,—", ""),
		["food_not_drug"] = new("A. BLOOMFIELD", "Consulting Chemist to the Trade · Maiden Lane", "A. BLOOMFIELD, Ph.D. (Heidelberg, he says)", "",
			"A card, with a sample label: BLOOMFIELD'S TABLE BEVERAGE (NOURISHING).", "Dear Sir,—", ""),
		["still_smell"] = new("", "Next door, the millinery, first floor", "H. ALBRECHT, Milliner", "",
			"Pinned to a bonnet, which smelled of the still.", "Sir,—", ""),
		["apprentice"] = new("", "Second year, the College of Pharmacy of the City of New-York", "W. ESCHER, Junior Class", "",
			"In a precise hand, with the grains written as a chemist writes them.", "Dear Sir,—", ""),
		["sign_painter"] = new("A. ROSSI", "Sign & Ornamental Painter · Gold Leaf a Specialty · Elizabeth Street", "A. ROSSI", "",
			"With a sketch of the sign in gold paint, which came off on the counter.", "Dear Sir,—", ""),
		["burglary"] = new("", "The station-house, Elizabeth Street", "P. LYNCH, Detective Sergeant", "",
			"Left by a detective in a bowler, who looked at the window and the till.", "Sir,—", ""),
		["sweatshop"] = new("H. KEMP & CO.", "Contractors · Shirtwaists & Wrappers · Walker Street, fourth floor", "H. KEMP", "",
			"On a contractor's billhead, with the piece rates on the back.", "Sir,—", ""),
		["gentleman_morphine"] = new("", "Washington Square, North · do not reply here", "A Gentleman", "",
			"On thick cream paper, the crest cut off with scissors.", "Sir,—", ""),
		["revenue_stamp"] = new("UNITED STATES INTERNAL REVENUE", "Office of the Collector · Second District, New-York", "By direction of the Collector, M. FARRELL, Deputy", "",
			"An official envelope, with a printed form for the tax.", "Sir,—", ""),
		["pushcart"] = new("", "Pushcart No. 412, at the corner of Bayard Street", "S. MARINO, with the cart", "",
			"Brought to the counter with a lemon, which was a gift.", "Dear Sir,—", ""),
		["photographer"] = new("J. HALE", "Photographic Artist · Cabinet Portraits · Bowery", "J. HALE", "",
			"With a sample portrait of a stranger in excellent whiskers.", "Dear Sir,—", ""),
		["obituary_desk"] = new("THE BOWERY GAZETTE", "City Desk · 18 Park Row", "for the City Editor, R. TOLL", "",
			"Brought by a copy boy, who waited.", "Sir,—", ""),
		["phrenologist"] = new("PROF. LYSANDER POOLE", "Practical Phrenologist · Heads Read, 25c. · Upstairs", "L. POOLE, Professor", "",
			"With a chart of the head, the organ of Vitality marked in red.", "Dear Neighbour,—", ""),
		["school_principal"] = new("THE GRAMMAR SCHOOL, MOTT STREET", "Office of the Principal", "E. WARDWELL, Principal", "",
			"On ruled school paper, in the hand that marks the copybooks.", "Sir,—", ""),
		// (end of events pass 1)
	};

	/// <summary>What each answer to the coroner risks, said plainly under the slip.</summary>
	private static readonly Dictionary<string, string> Risks = new()
	{
		["inquest.testify"] = "Your word helps a mild bottle, and hurts a strong one: the jury's chemist tests whatever you bring. If they name your bottle, suspicion rises 24 and your standing falls.",
		["inquest.stay_silent"] = "No chemistry is heard, which suits a strong bottle. If the jury names your bottle anyway, suspicion rises 24 and your standing falls.",
		["inquest.bribe_coroner"] = "The verdict you pay for. His fee rises with every envelope and with the talk in the ward, and an envelope can come to light: a bought verdict found out is worse than a jury's.",
		["grateful_letter.print_testimonial"] = "Standing in the ward +3. A testimonial in print is half of what the Gazette asks before it sells you space; the other half is $100 put by.",
		["grateful_letter.file_away"] = "Nothing gained, nothing risked.",
		["diploma_mill.buy_diploma"] = "$50, and you may style yourself Doctor: a new claim for the type case. You become better known, for better or worse.",
		["diploma_mill.decline"] = "Costs nothing.",
		["sanitary_inspector.pay_fee"] = "$3, and he is gone by dinner. You become a little better known at the Department.",
		["sanitary_inspector.let_him_look"] = "Free, but the ward sees him at it: standing −2.",
		["rival_shop_opens.spread_rumour"] = "Standing +1 for now, and you become better known, which the coroner may notice too.",
		["rival_shop_opens.ignore"] = "Standing −3: the ward takes his side.",
		["temperance_picket.donate"] = "$5, and the ladies move on: suspicion −5.",
		["temperance_picket.ignore_them"] = "Free, but the hymns cost you standing −2.",
		["board_of_health.pay_fine"] = "$30, and the complaint is answered: suspicion −10.",
		["board_of_health.surrender_stock"] = "Every bottle on the shelf is taken away; suspicion −20.",
		["customer_wants_stronger.promise_stronger"] = "Standing +1 with the regular. Keeping the promise is up to the formula book.",
		["customer_wants_stronger.recommend_rest"] = "Good advice, and nothing changes.",
		["rival_price_war.handbills"] = "$10, and Feeney takes nothing from you.",
		["rival_price_war.cry_him_down"] = "Free, and Feeney takes nothing, but suspicion +4 and standing −2.",
		["rival_price_war.let_him_try"] = "For four months he takes two callers in five from any bottle dearer than 65c.",
		["landlord_rent.buy_the_lease"] = "$60 now, and the rent falls a dollar a month, for good.",
		["landlord_rent.smaller_premises"] = "Rent a dollar under the old, for good, but room for eight fewer regulars.",
		["landlord_rent.pay_the_rise"] = "Rent up a dollar a month, for good.",
		["spirits_dearer.outside_distiller"] = "$6 now, and spirit stays at the old price.",
		["spirits_dearer.pay_the_new_price"] = "Grain spirit costs double for six months.",
		["benefit_society.subscribe"] = "$1.50 a month for a year, and about five more callers a month.",
		["benefit_society.decline_politely"] = "Nothing changes.",
		// Events pass 1: the letters of the long middle (playtests/events-pass-1.md).
		["landlord_roof.pay_slater"] = "$5, and the roof is sound.",
		["landlord_roof.rent_it"] = "Rent up 50c a month for a year: $6 in all, a little at a time.",
		["landlord_roof.pan_under"] = "Free, but the customers see the pan: standing −1, suspicion +1.",
		["doctor_arrangement.accept"] = "$1 a month for a year, and about three more callers a month, on his prescriptions.",
		["doctor_arrangement.decline"] = "Free, but he speaks against your tonic at the bedside: suspicion +2.",
		["parish_bazaar.donate_dozen"] = "$3; standing +1, and about three more callers a month for two months, sent from the pulpit.",
		["parish_bazaar.send_dollar"] = "$1, and standing +1.",
		["parish_bazaar.regrets"] = "Free, but the parish notices: standing −1.",
		["police_collection.give_five"] = "$5; suspicion −4. Somebody keeps a book of who gave.",
		["police_collection.refuse"] = "Free, but the precinct takes an interest: suspicion +2.",
		["gazette_canvasser.buy_notice"] = "$4, and about four more callers a month for two months.",
		["gazette_canvasser.wait"] = "Costs nothing.",
		["board_of_pharmacy.register"] = "$5; standing +1: a framed certificate behind the counter.",
		["board_of_pharmacy.buy_certificate"] = "$3, for a certificate nobody looks at closely. You become better known; suspicion +2.",
		["board_of_pharmacy.file_it"] = "Free, but the Board marks you down: suspicion +5.",
		["blizzard_1888.give_credit"] = "About $4 that never comes back; standing +1, and the talk in the ward softens.",
		["blizzard_1888.raise_prices"] = "About $5 more this month; standing −3, and you become better known.",
		["blizzard_1888.cash_only"] = "Standing −1: the Committee remembers.",
		["grippe_1889.placards"] = "$2, and about six more callers a month for two months; suspicion +4.",
		["grippe_1889.bed_rest"] = "Standing +1.",
		["jobber_exclusive.sign"] = "$10 down, and every ingredient a tenth cheaper for a year.",
		["jobber_exclusive.stay_free"] = "Costs nothing.",
		["medicine_show_offer.sign_on"] = "$15 now. What comes back depends on the summer, and on the Professor.",
		["medicine_show_offer.decline"] = "Costs nothing.",
		["medicine_show_returns.take_money"] = "$21: the summer paid $6 over its cost.",
		["medicine_show_returns.ask_for_books"] = "$29 when he finds what he overlooked; but he tells the canal towns about you: standing −1, and you become better known.",
		["kehoe_thanks.send_syrup"] = "A bottle on the house (30c); standing +1. She will be back for more.",
		["kehoe_thanks.send_to_dispensary"] = "Standing −1 for now: she hoped you had something.",
		["kehoe_more.stronger"] = "She buys more: about $1 this month. Standing +1, suspicion +2.",
		["kehoe_more.warn_her"] = "Suspicion +3: the doctor asks where the syrup came from. The talk in the ward softens.",
		["kehoe_more.same_again"] = "About 50c this month.",
		["kehoe_last.pay_doctor"] = "$10; suspicion −2, and the talk in the ward softens a little.",
		["kehoe_last.see_the_doctor"] = "$25; suspicion −6, but you become better known, and the top floor knows what you did.",
		["kehoe_last.say_nothing"] = "Suspicion +5, and the ward hears of it.",
		["kehoe_well.ask_to_print"] = "Standing +1, and a testimonial in print; you become a little better known.",
		["kehoe_well.thank_her"] = "About two more callers a month for six months: Mrs. Doyle, and her sister.",
		["stowe_tract.answer_truly"] = "Standing +1, suspicion +2: the League now knows what is in it.",
		["stowe_tract.purely_vegetable"] = "Standing +1. The League keeps its sheets.",
		["stowe_tract.wrap_a_bottle"] = "Suspicion +1.",
		["ward_boss_favor.picture"] = "Suspicion −8, and you become better known. The Alderman's friends are remembered, both ways.",
		["ward_boss_favor.decline"] = "Suspicion +2: the Sanitary Bureau hears you have no friends.",
		["ward_boss_due.supply"] = "$15; suspicion −6. Your name is in the Alderman's book.",
		["ward_boss_due.bottles_only"] = "$6 in bottles; suspicion −1, and you become a little better known.",
		["ward_boss_due.refuse"] = "Suspicion +8: the Sanitary Bureau remembers you after all.",
		["feeney_boy.raise"] = "$2 a month for a year, and the jug stays a secret.",
		["feeney_boy.let_him_go"] = "Free. Feeney gets a sweeper, and perhaps a receipt.",
		["feeney_copy.lawyer"] = "$20, and the bottles come out of his window. Standing +1.",
		["feeney_copy.cry_imitation"] = "For eight months he takes one caller in seven from any bottle dearer than 45c. Suspicion +2; you become better known.",
		["feeney_copy.let_him"] = "For eight months he takes two callers in five from any bottle dearer than 45c.",
		["feeney_inquest.disown"] = "Standing +1, suspicion +3: your receipt is talked of at an inquest. You become better known.",
		["feeney_inquest.buy_feeney_out"] = "$60. Feeney's shop shuts, and about two of his callers a month come to you, for good. Suspicion −2.",
		["feeney_inquest.say_nothing"] = "Suspicion +4, and the ward hears that your receipt killed a man.",
		["pawn_ticket.redeem_it"] = "$1.50; standing +1, and the talk in the ward softens a little.",
		["pawn_ticket.sell_ticket"] = "$1; standing −2, and the ward hears of it.",
		["pawn_ticket.drawer"] = "Nothing changes. The ring stays pledged.",
		["widow_account.close_account"] = "Standing +1, and the talk in the ward softens a little.",
		["widow_account.take_watch"] = "$3; standing −2, and the ward hears of it.",
		["widow_account.ten_cents"] = "About $2 comes in, in dimes.",
		["soda_fountain_offer.install"] = "$60, and about two more callers a month, for good.",
		["soda_fountain_offer.decline"] = "Costs nothing.",
		["undertaker.shake_hands"] = "About two more callers a month for six months, on his word; standing −2, and you become better known.",
		["undertaker.show_the_door"] = "Costs nothing.",
		["electric_light.wire_window"] = "$35, then 50c a month for the current; about two more callers a month, for good.",
		["electric_light.gas_will_do"] = "Costs nothing.",
		["reporter_calls.show_him"] = "Standing +1, and you become better known. What he writes depends on what he sees.",
		["reporter_calls.pay_him"] = "$5. A paid man stays paid only so long.",
		["reporter_calls.show_the_door"] = "Free, but a refused reporter writes anyway.",
		["reporter_proofs.buy_the_head"] = "$25, and the piece runs as a puff: suspicion −2. He will remember the price.",
		["reporter_proofs.pull_the_ads"] = "The editor kills the piece, but Park Row hears why: suspicion +3, and you become better known.",
		["reporter_proofs.let_it_run"] = "Suspicion +8, standing −3, and the ward reads it.",
		["reporter_kind.send_patients"] = "About four more callers a month for three months; standing +1, suspicion +2, and you become better known.",
		["reporter_kind.leave_it"] = "Standing +1.",
		["stowe_chemist.pay_for_hall"] = "$20; suspicion −4, and the certificate stays in a drawer.",
		["stowe_chemist.own_chemist"] = "$12 for a certificate of your own; you become better known, suspicion +1.",
		["stowe_chemist.let_them_print"] = "Suspicion +6, standing −3.",
		["stowe_petition.sign_first"] = "Standing +2, and you become better known; the wholesale houses charge a tenth more for a year.",
		["stowe_petition.decline"] = "Costs nothing.",
		["proprietary_invite.join"] = "$25; suspicion −3. The Association will ask for more when there is a bill.",
		["proprietary_invite.decline"] = "Costs nothing.",
		["red_clause.add_clause"] = "Suspicion −2, and you become a little better known. The papers you advertise in now have an interest in the law.",
		["red_clause.plain_contract"] = "Costs nothing.",
		["cholera_1892.cholera_card"] = "About eight more callers a month for two months; suspicion +6.",
		["cholera_1892.post_notice"] = "Standing +1, suspicion −2.",
		["magazine_household.household_cordial"] = "$20, and about four more callers a month for four months; you become a little better known.",
		["magazine_household.decline"] = "Costs nothing.",
		["panic_relief.honour_tickets"] = "About $20 of medicine at cost; standing +1, and the talk in the ward softens.",
		["panic_relief.decline"] = "Standing −2.",
		["panic_credit.settle"] = "$30 now.",
		["panic_credit.on_credit"] = "Every ingredient costs a tenth more for six months.",
		["lexow_subpoena.testify"] = "Standing +2, suspicion −6, and you become well known. The ward's friends will not forget it.",
		["lexow_subpoena.forget"] = "Suspicion +5, standing −2: the papers print who remembered nothing.",
		["lexow_subpoena.catskills"] = "$40, and the Committee sits without you. You become a little better known.",
		["sunday_closing.sunday_bitters"] = "About $40 over the summer; suspicion +6, standing −1. The Board has plain-clothes men too.",
		["sunday_closing.by_the_bottle"] = "About $10; suspicion +2.",
		["sunday_closing.keep_sabbath"] = "Standing +1.",
		["wheelmen.club_price"] = "About $10 in dimes off, and three more callers a month for six months.",
		["wheelmen.decline"] = "Costs nothing.",
		["trolley_cards.take_cards"] = "$4 a month for a year, and about four more callers a month.",
		["trolley_cards.decline"] = "Costs nothing.",
		["bicycle_face.endorsement"] = "$15, and about four more callers a month for four months; you become better known.",
		["bicycle_face.get_off"] = "Standing +1.",
		["state_chemist.observations"] = "$30; suspicion −5, and you become better known in Albany.",
		["state_chemist.let_it_print"] = "Suspicion +4: juries read the report. Standing +1, for having nothing to say.",
		["lodging_house.by_the_gallon"] = "About $40; suspicion +4, and the Bowery talks.",
		["lodging_house.hire_a_watchman"] = "Costs nothing.",
		["tammany_back.pay_club"] = "$100, and the list is mislaid.",
		["tammany_back.to_the_papers"] = "Standing +1, suspicion +4, and you become better known.",
		["tammany_back.let_them_come"] = "Suspicion +10, standing −1.",
		["volunteers_1898.as_a_gift"] = "$20; standing +1, and the talk in the ward softens.",
		["volunteers_1898.at_cost"] = "About $5 of margin given up; standing +1.",
		["volunteers_1898.cannot_spare"] = "Standing −2.",
		["army_contract.sign_certificate"] = "$80 in royalties; suspicion +4, and you become better known. Your name is on a War Department file.",
		["army_contract.decline"] = "Costs nothing.",
		["army_inquiry.blame_contractor"] = "$20 for the journey; suspicion +2, standing +1.",
		["army_inquiry.write_a_hope"] = "Suspicion +5, and you become better known.",
		["army_inquiry.say_nothing"] = "Suspicion +8, standing −2.",
		["farm_wife.send_drops"] = "$1 by post; suspicion +2, and you become a little better known.",
		["farm_wife.write_honestly"] = "Standing +1.",
		["testimonial_broker.buy_hundred"] = "$6; standing +1 while they are believed, suspicion +2, and you become better known.",
		["testimonial_broker.decline"] = "Costs nothing.",
		["heroin_traveller.fair_trial"] = "Every opiate a third cheaper for a year. The new article goes in your bottle only if you put it there.",
		["heroin_traveller.decline"] = "Costs nothing.",
		["letter_broker.sell_letters"] = "$15; suspicion +2. Your correspondents will hear from strangers, and say so.",
		["letter_broker.burn_them"] = "Costs nothing.",
		["ice_trust.pay_the_price"] = "$25 for the summer's ice.",
		["ice_trust.jersey_ice"] = "$10, for ice nobody has tested: suspicion +2.",
		["ice_trust.shut_fountain"] = "The fountain's two callers a month stay away for four months.",
		["cut_rate.sign_plan"] = "$30, and the cutters lose your bottle.",
		["cut_rate.let_them_cut"] = "For six months one caller in four goes to the cutters, for any bottle dearer than 50c.",
		["grimsby_belt.partner"] = "About three more callers a month for six months; suspicion +2, and you become better known.",
		["grimsby_belt.decline"] = "Costs nothing.",
		["tenement_dept.fireproof"] = "$80, and the order is satisfied. Standing +1.",
		["tenement_dept.ask_inspector"] = "$20; suspicion +2, and you become better known. The new Department is not the old Bureau.",
		["tenement_dept.show_cause"] = "Suspicion +6.",
		["bureau_sample.special_batch"] = "Suspicion −3, and you become a little better known. The Bureau buys its own bottles too.",
		["bureau_sample.from_the_shelf"] = "Standing +1, suspicion +3.",
		["bureau_sample.mislay_it"] = "Suspicion +2.",
		["radium_water.buy_licence"] = "$40, and about four more callers a month for six months; you become better known.",
		["radium_water.decline"] = "Costs nothing.",
		["motor_car.settle"] = "$20.",
		["motor_car.tell_the_gazette"] = "Standing +1, you become better known, and about eight callers come to see the hole this month.",
		["subway_signs.take_signs"] = "$12 a month for a year, and about six more callers a month.",
		["subway_signs.decline"] = "Costs nothing.",
		["muckraker.send_formula"] = "Standing +1, suspicion +4: what is in it will be in print. You become better known.",
		["muckraker.libel_letter"] = "$50; suspicion −2, and you become better known: a threat is a story too.",
		["muckraker.no_reply"] = "Suspicion +5: 'did not reply' reads badly in print.",
		["reporter_returns.pay_again"] = "$100; suspicion −3. He promises it is the last time.",
		["reporter_returns.give_a_rival"] = "He writes up the rival, and the whole trade grows hotter: suspicion +3, standing −1. The rival guesses who, and for six months takes three callers in ten from any bottle dearer than 50c.",
		["reporter_returns.let_him"] = "Suspicion +12, standing −4: you are in the magazine.",
		["ama_council.submit"] = "Standing +1, suspicion +3: the Council will know what is in it. About two more callers a month for a year, on physicians' advice.",
		["ama_council.decline"] = "Standing −1: the Journal prints the names of those who declined.",
		["pa_war_chest.pay_share"] = "$150; suspicion −6.",
		["pa_war_chest.resign"] = "Standing +1, and you become a little better known.",
		["pa_war_chest.with_the_bills"] = "Suspicion +2: the Association notes who did not pay.",
		["red_clause_called.write_editors"] = "Suspicion −8, but it is the kind of letter that gets printed: standing −2, and you become better known.",
		["red_clause_called.hold_off"] = "Costs nothing.",
		["congress_member.contribute"] = "$100; suspicion −6.",
		["congress_member.for_the_bill"] = "Standing +1, and you become better known.",
		["congress_member.no_views"] = "Costs nothing.",
		["stowe_washington.go_with_her"] = "Standing +1, suspicion +4: the committee will know what is in it. You become better known.",
		["stowe_washington.donation"] = "$50; suspicion −4. She will say you were generous.",
		["stowe_washington.let_her_say"] = "Suspicion +3.",
		["label_lawyer.retain"] = "$75; suspicion −4, and you become a little better known.",
		["label_lawyer.plain_label"] = "$10 for a new plate; standing +1.",
		["label_lawyer.wait_and_see"] = "Suspicion +4.",
		["food_not_drug.become_food"] = "$30; suspicion −3, and you become better known.",
		["food_not_drug.stay_medicine"] = "Standing +1.",
		["still_smell.put_up_pipe"] = "$6; standing +1.",
		["still_smell.buy_bonnet"] = "$3 for a bonnet; she complains to her customers instead: suspicion +1.",
		["still_smell.let_her_complain"] = "Suspicion +2, standing −1.",
		["apprentice.take_him_on"] = "$4 a month for a year. He weighs every batch, and pours away a bad one before it reaches the shelf. He also sees what goes in it: suspicion +1.",
		["apprentice.no_room"] = "Costs nothing.",
		["sign_painter.gilt_sign"] = "$12, and about two more callers a month, for good.",
		["sign_painter.touch_up"] = "$2, and about one more caller a month for two years.",
		["sign_painter.leave_it"] = "Costs nothing.",
		["burglary.consideration"] = "The till held $8; you pay $4 to have it found, and are $4 down. Somebody keeps a book of who paid.",
		["burglary.bars"] = "The $8 in the till, and $3 for bars; no one comes in that way again.",
		["burglary.write_off"] = "The $8 in the till is gone.",
		["sweatshop.sell_by_gross"] = "About $30; suspicion +3, and the girls' families talk.",
		["sweatshop.refuse"] = "Costs nothing.",
		["gentleman_morphine.supply"] = "About $25; suspicion +3.",
		["gentleman_morphine.refer_him"] = "Standing +1.",
		["revenue_stamp.pay_tax"] = "$25; suspicion −2.",
		["revenue_stamp.see_deputy"] = "$8; suspicion +2, and you become a little better known.",
		["revenue_stamp.dispute"] = "Suspicion +3: a deputy will come and drink some.",
		["pushcart.hire_cart"] = "50c a month for six months, and about two more callers a month; suspicion +1.",
		["pushcart.decline"] = "Costs nothing.",
		["photographer.sit_for_it"] = "$5; standing +1, you become better known, and about one more caller a month, for good.",
		["photographer.decline"] = "Costs nothing.",
		["obituary_desk.buy_space"] = "$10; suspicion −3.",
		["obituary_desk.write_denial"] = "Suspicion +1, standing −1, and you become better known: a denial is a paragraph too.",
		["obituary_desk.let_it_print"] = "Suspicion +2, and the ward reads it.",
		["phrenologist.agree"] = "About two more callers a month for six months; standing −1, and you become a little better known.",
		["phrenologist.decline"] = "Costs nothing.",
		["school_principal.no_boys"] = "About $2 of trade lost; standing +1.",
		["school_principal.smaller_bottle"] = "About $2; suspicion +3, and the mothers hear of it.",
		["school_principal.file_it"] = "Suspicion +1, and the mothers hear of it.",
		// (end of events pass 1)
	};

	/// <summary>Where a printed letter is dated from, when it is not New-York.</summary>
	private static readonly Dictionary<string, string> Places = new()
	{
		["diploma_mill"] = "Cincinnati",
		["medicine_show_offer"] = "Elmira",
		["medicine_show_returns"] = "Elmira",
		["state_chemist"] = "Albany",
		["army_contract"] = "Washington",
		["army_inquiry"] = "Washington",
		["bureau_sample"] = "Washington",
		["radium_water"] = "Newark",
		["ama_council"] = "Chicago",
		["congress_member"] = "Washington",
	};

	/// <summary>"one in 20", "three in 10": odds a reader can feel.</summary>
	private static string Chance(double p) =>
		p < 0.05 ? "fewer than one in 20" : p < 0.1 ? "about one in 10" : $"about {Math.Clamp((int)Math.Round(p * 10), 1, 9)} in 10";

	public LettersDesk(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 14);
		Refresh();
	}

	public void Refresh()
	{
		var keep = Print.FocusIndex(this);
		RefreshBody();
		Print.RestoreFocus(this, keep);
	}

	private void RefreshBody()
	{
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		var d = _game.Desk();
		var top = new HBoxContainer();
		top.AddChild(Print.Label("THE MORNING'S POST", Print.Rye, 22, color: Print.Paper));
		top.AddChild(Print.Label(d.Letters.Count == 0 ? "nothing for you" : d.Letters.Count == 1 ? "one letter" : $"{d.Letters.Count} letters", Print.FellIt, 17, HorizontalAlignment.Right, Print.Paper));
		// a band of ink, so the heading never sits on the proof behind the shade
		AddChild(Print.Panel(top, Print.Box(Print.Ink, Print.Ink, 0, 8)));
		if (_note != null) AddChild(Print.Slip(Print.Body($"[i]{Print.Escape(EditionWriter.Set(_note))}[/i]", 15, justify: false), 10));
		if (d.Letters.Count == 0)
		{
			AddChild(Envelope());
			return;
		}
		// each letter keeps the date it arrived: fixed by the letter itself, never after the proof
		var press = EditionWriter.PublicationDate(d.Year, d.Month);
		for (var i = 0; i < d.Letters.Count; i++)
		{
			var h = 0u;
			foreach (var ch in d.Letters[i].InstanceId) h = (h ^ ch) * 16777619u;
			AddChild(Sheet(d.Letters[i], press.AddDays(-2 - (int)(h % 12)), i, Served(d.Turn) + d.Letters.Take(i + 1).Count(x => x.EventId == "inquest")));
		}
	}

	private Control Envelope()
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);
		v.AddChild(Print.Label("A CIRCULAR", Print.Rye, 18, HorizontalAlignment.Center));
		v.AddChild(Print.Body("[center][i]Dr. Grimsby begs to announce his Galvanic Belt, which restores Lost Vigor through every Nerve. Sent on thirty days' trial.[/i][/center]", 15, justify: false));
		v.AddChild(Print.Body("[center]Nothing else in the post this morning.[/center]", 14, justify: false));
		var s = Print.Slip(v, 18);
		s.RotationDegrees = -1f;
		return s;
	}

	/// <summary>Inquest notices answered so far this month (the month's turn, and the count).</summary>
	private static (int Turn, int Count) _served;
	private static int Served(int turn) => _served.Turn == turn ? _served.Count : 0;

	private Control Sheet(Letter l, DateTime date, int index, int inquestNo = 0)
	{
		var head = Heads.GetValueOrDefault(l.EventId) ?? new Head("", "", "Yours, &c.", "", "", "Sir,—", "");
		var at = l.Title.IndexOf("Death of ", StringComparison.Ordinal);
		var concerns = at >= 0 ? l.Title[(at + 9)..].Replace("the Infant of", "the infant child of").Replace("an Unknown", "an unknown") : "";
		if (concerns.StartsWith("an unknown", StringComparison.Ordinal)) concerns = concerns.ToLowerInvariant();
		var body = head.Voice.Length > 0 ? head.Voice.Replace("{name}", concerns.Length > 0 ? concerns : "a person") : l.Body;
		// the Clerk keeps count: a busy month's summonses say so
		if (l.EventId == "inquest" && inquestNo >= 2)
			body += $" This is the {Nth(inquestNo)} such notice the Clerk has had the honour to serve on you this month.";
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 6);

		// the clerk's docket: how the letter came
		if (head.Docket.Length > 0) v.AddChild(Print.Label(head.Docket, Print.FellIt, 13, color: new Color(Print.Ink, 0.85f)));

		// the letterhead: printed, or nothing (a hand-written note)
		var lh = new HBoxContainer();
		lh.AddThemeConstantOverride("separation", 14);
		var names = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		names.AddThemeConstantOverride("separation", 0);
		if (head.Sender.Length > 0)
		{
			names.AddChild(Print.Label(head.Sender, Print.Rye, 17));
			names.AddChild(Print.Label(head.Address, Print.FellIt, 13));
		}
		else names.AddChild(Print.Label(head.Address, Print.FellIt, 15));
		lh.AddChild(names);
		if (head.Seal.Length > 0) lh.AddChild(Seal(head.Seal));
		v.AddChild(lh);
		if (head.Sender.Length > 0) v.AddChild(Print.Rule(2, 0.8f, 2));
		// a printed letter is dated; a note pushed under the door knows only the day
		var note = head.Sender.Length == 0;
		v.AddChild(Print.Label(note ? date.DayOfWeek + "." : $"{Places.GetValueOrDefault(l.EventId, "New-York")}, {date.ToString("MMMM d", CultureInfo.InvariantCulture)}{Ordinal(date.Day)}, {date.Year}.", Print.FellIt, 14, HorizontalAlignment.Right));
		v.AddChild(Print.Label("Re: " + EditionWriter.Set(l.Title), Print.Rye, 15));
		v.AddChild(Print.Body(note ? $"[i]{Print.Escape(EditionWriter.Set(head.Salute + body))}[/i]" : Print.Escape(EditionWriter.Set(head.Salute + body)), note ? 17 : 16, justify: false));
		v.AddChild(Print.Label(l.From != null ? $"Your grateful servant, {l.From}" : head.Sign, Print.FellIt, 14, HorizontalAlignment.Right));

		// the replies, each a slip to send back
		v.AddChild(Print.Label("YOUR REPLY", Print.Fell, 12));
		Control? sheet = null;
		foreach (var (id, text) in l.Choices)
		{
			var b = Print.Button("☞  " + EditionWriter.Set(text), 14, sound: null);
			b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			b.Alignment = HorizontalAlignment.Left;
			var choice = id;
			var said = text;
			b.Pressed += () => Answer(sheet!, l.InstanceId, choice, said);
			v.AddChild(b);
			// the reply and the line under it come into view together, with room to spare
			Control? riskLine = null;
			b.FocusEntered += () => Callable.From(() =>
			{
				for (Node? n = b; n != null; n = n.GetParent())
					if (n is ScrollContainer sc)
					{
						sc.EnsureControlVisible(riskLine ?? b);
						var bottom = (riskLine ?? b).GetGlobalRect().End.Y - sc.GetGlobalRect().End.Y;
						if (bottom > -30) sc.ScrollVertical += (int)(bottom + 30);
						break;
					}
			}).CallDeferred();
			if (Risks.TryGetValue($"{l.EventId}.{id}", out var risk))
			{
				string? pencil = null;
				// the verdict is given as soon as the reply goes: say what the odds are now
				if (l.Odds.TryGetValue(id, out var p))
					pencil = id == "bribe_coroner"
						? $"Pencilled in the margin: {Chance(p)} that it comes out. —J.D."
						: $"Pencilled in the margin: {Chance(p)} for naming" +
							(l.Odds.TryGetValue(id == "testify" ? "stay_silent" : "testify", out var q) && Chance(q) == Chance(p) ? ", speak or not" : "") + ". The jury sits at once. —J.D.";
				var line = Print.Label(risk, Print.FellIt, 15, color: Print.Ink);
				riskLine = line;
				v.AddChild(line);
				if (pencil != null)
				{
					var margin = Print.Label(pencil, Print.FellIt, 14, HorizontalAlignment.Right, Print.Ink);
					margin.RotationDegrees = -0.8f;
					riskLine = margin;
					v.AddChild(margin);
				}
			}
		}
		var s = Print.Slip(v, 18);
		// notes lie crooked on the desk; printed letters nearly straight
		s.RotationDegrees = (index % 2 == 0 ? -0.6f : 0.5f) * (head.Sender.Length == 0 ? 3f : 1f);
		sheet = s;
		return s;
	}

	private void Answer(Control sheet, string instance, string choice, string said)
	{
		var d0 = _game.Desk();
		var before = d0.CashCents;
		var heat0 = d0.Heat;
		var inquest = d0.Letters.FirstOrDefault(x => x.InstanceId == instance)?.EventId == "inquest";
		var err = _game.Resolve(instance, choice);
		// the Clerk counts the notices he has served this month, answered ones included
		if (inquest && err == null) _served = (d0.Turn, Served(d0.Turn) + 1);
		if (err != null) { _note = "Refused: " + err; Refresh(); return; }
		var d1 = _game.Desk();
		var after = d1.CashCents;
		var money = after == before ? "" : after < before ? $" It cost {EditionWriter.Money(before - after)}." : $" It brought in {EditionWriter.Money(after - before)}.";
		// say what happened at once; only what has not happened yet is left to the paper
		var stockGone = d0.Formulas.Sum(f => f.Stock) - d1.Formulas.Sum(f => f.Stock);
		var talk = d1.Heat - heat0;
		var standing = d1.Reputation - d0.Reputation;
		var now = new List<string>();
		if (stockGone > 0) now.Add($"{stockGone} bottles are gone from the shelf");
		if (Math.Abs(talk) >= 1) now.Add($"suspicion {(talk > 0 ? "rises" : "falls")} {Math.Abs(talk):0}");
		if (Math.Abs(standing) >= 1) now.Add($"your standing {(standing > 0 ? "rises" : "falls")} {Math.Abs(standing):0}");
		var outcome = !inquest ? (now.Count > 0 ? " At once: " + string.Join("; ", now) + "." : "") + " Anything further will be in the next edition."
			// a bribe can come out at once: then say so, not that the verdict was bought cleanly
			: choice == "bribe_coroner" && d1.Heat > heat0 + 10 ? $" The coroner took your envelope, and the ward heard of it before the jury sat. Suspicion is now {d1.Heat:0} of 100{(standing <= -1 ? $", and your standing falls {Math.Abs(standing):0}" : "")}. The paper will have the rest."
			: choice == "bribe_coroner" ? " The coroner has your envelope; the verdict will be the one you paid for."
			: d1.Heat > heat0 + 10 ? $" The jury sat at once and named your bottle. Suspicion is now {d1.Heat:0} of 100. The paper will have the rest."
			: " The jury sat at once and found nothing against you. The paper will have the verdict.";
		_note = $"Sent: “{said}”{money}{outcome}";
		Main.Guard(800);

		// the rubber stamp comes down, and the letter is folded away
		Sfx.Play("sheet", 0f, 0.05f, 0.8f);
		var stamp = Print.Panel(Print.Label("ANSWERED", Print.Rye, 30, HorizontalAlignment.Center, new Color(Print.Ink, 0.75f), wrap: false), Print.Box(new Color(0, 0, 0, 0), new Color(Print.Ink, 0.75f), 4, 6));
		stamp.MouseFilter = MouseFilterEnum.Ignore;
		stamp.Position = new Vector2(sheet.Size.X * 0.5f - 110, 30);
		stamp.RotationDegrees = -14;
		stamp.Scale = new Vector2(1.6f, 1.6f);
		sheet.AddChild(stamp);
		stamp.TopLevel = false;
		var t = sheet.CreateTween();
		t.TweenProperty(stamp, "scale", Vector2.One, 0.12f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		t.TweenInterval(0.45f);
		t.TweenCallback(Callable.From(() => { sheet.PivotOffset = new Vector2(sheet.Size.X / 2, 0); Sfx.Play("slip", -4f); }));
		t.TweenProperty(sheet, "scale:y", 0.05f, 0.25f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		t.Parallel().TweenProperty(sheet, "modulate:a", 0f, 0.25f);
		t.TweenCallback(Callable.From(() =>
		{
			Refresh();
			Changed?.Invoke();
			// the next letter's first reply takes the keyboard, scrolled into view
			Callable.From(() =>
			{
				if (!IsInsideTree()) return;
				var first = Print.Focusables(this).OfType<Button>().FirstOrDefault(x => !x.Disabled);
				if (first == null) return;
				first.GrabFocus();
				for (Node? n = this; n != null; n = n.GetParent())
					if (n is ScrollContainer sc) { sc.EnsureControlVisible(first); break; }
			}).CallDeferred();
		}));
	}

	/// <summary>
	/// The sender's engraved seal (tools/make_seals.py): a roundel with its legend round the rim,
	/// printed in the letterhead's corner.
	/// </summary>
	public static Control Seal(string cut, int size = 104)
	{
		var tex = Print.Cut(cut);
		var r = new TextureRect
		{
			Texture = tex,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(size, size),
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
			PivotOffset = new Vector2(size / 2f, size / 2f),
			RotationDegrees = -8,
		};
		return r;
	}

	private static string Nth(int n) => n switch { 2 => "second", 3 => "third", 4 => "fourth", 5 => "fifth", 6 => "sixth", 7 => "seventh", 8 => "eighth", _ => $"{n}th" };

	private static string Ordinal(int d) => (d % 100) is 11 or 12 or 13 ? "th" : (d % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
}
